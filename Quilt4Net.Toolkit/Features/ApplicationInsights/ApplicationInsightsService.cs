using System.Collections.Concurrent;
using System.Diagnostics;
using Azure.Monitor.Query.Logs;
using Azure.Monitor.Query.Logs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Quilt4Net.Toolkit.Features.Diagnostics;
using Tharga.Cache;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsService : IApplicationInsightsService
{
    /// <summary>
    /// Canonical KQL fragment for projecting the Environment column from any AppTraces /
    /// AppExceptions / AppRequests row. Reads OTel resource attribute first (what
    /// <c>AddQuilt4NetLogging</c> emits), then the legacy <c>AspNetCoreEnvironment</c>
    /// scope tag. Centralised so all log queries stay in sync.
    /// </summary>
    internal const string EnvironmentProjection =
        "coalesce(tostring(_p[\"deployment.environment\"]), tostring(_p[\"AspNetCoreEnvironment\"]))";

    /// <summary>
    /// Canonical KQL fragment for projecting the Version column from any AppTraces /
    /// AppExceptions / AppRequests row. Reads OTel attribute first (what the per-record
    /// processor in <c>AddQuilt4NetLogging</c> writes), then a legacy <c>Version</c>
    /// scope key, then the AI built-in <c>AppVersion</c> column.
    /// </summary>
    internal const string VersionProjection =
        "coalesce(tostring(_p[\"service.version\"]), tostring(_p[\"Version\"]), tostring(AppVersion))";

    /// <summary>
    /// Canonical summarize clause for the <c>GetSummaries</c> queries. Buckets by
    /// <c>Fingerprint</c> only and takes representative samples for the descriptive columns,
    /// matching the post-fetch C# dedupe (one row per fingerprint, last-seen wins).
    ///
    /// Previously the summarize key was <c>(Fingerprint, Message, Environment, Application, SeverityLevel)</c>,
    /// which split a single fingerprint into N rows whenever the message text varied (always, for
    /// formatted trace logs) and could exceed Kusto's 64MB result-set limit. Aligning the server-side
    /// grouping with what the UI actually shows shrinks the result set by orders of magnitude and
    /// matches what <c>GetSummaries</c>'s C# layer was already enforcing post-fetch.
    /// </summary>
    internal const string SummarizeByFingerprint = @"| summarize
    Count = count(),
    LastTimeGenerated = max(TimeGenerated),
    Message = take_any(Message),
    Environment = take_any(Environment),
    Application = take_any(Application),
    SeverityLevel = max(SeverityLevel)
    by Fingerprint
| order by LastTimeGenerated desc";

    private static readonly LogsQueryOptions _probeOptions = new() { ServerTimeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    /// Used by <c>GetSummariesInternal</c>. <c>AllowPartialErrors = true</c> means the SDK returns
    /// truncation warnings via <c>LogsQueryResult.Error</c> instead of throwing — defence-in-depth
    /// for any future query that grows past the 64MB limit despite the tightened summarize clause.
    /// </summary>
    private static readonly LogsQueryOptions _summariesQueryOptions = new() { AllowPartialErrors = true };

    /// <summary>
    /// Pick-the-winner stage for <c>GetVersionMatrixAsync</c>'s KQL. Expects two <c>let</c>-bound
    /// inputs upstream: <c>startup</c> (rows where <c>Quilt4NetStartup=true</c>) and <c>fallback</c>
    /// (rows from any of AppTraces/AppExceptions/AppRequests).
    ///
    /// Previously the implementation was <c>union startup, fallback | summarize arg_max(TimeGenerated, Version, Source) by ApplicationName, Environment</c>,
    /// which sorts every row by timestamp regardless of source. Any regular log entry emitted *after*
    /// <c>Quilt4NetStartupHostedService.StartAsync</c> wins the <c>arg_max</c> — and apps log
    /// continuously, so the <c>Source = "Startup"</c> row was effectively unreachable. The doc/XML
    /// promised a "Startup fast path" but the implementation gave no preference.
    ///
    /// New shape: pick the latest row per source first, then <c>leftanti</c>-join Log against Startup
    /// to keep Log rows only where no Startup row exists for that (App, Env). The result is that any
    /// Startup row beats any Log row for the same (App, Env) pair, regardless of timestamps.
    /// </summary>
    internal const string VersionMatrixPickStartupPreferred = @"let startup_pick = startup
| extend Environment = iff(isempty(Environment), ""(unknown)"", Environment)
| extend Machine = iff(isempty(Machine), ""(unknown)"", Machine)
| summarize arg_max(TimeGenerated, Version, Source) by ApplicationName, Environment, Machine;
let log_pick = fallback
| extend Environment = iff(isempty(Environment), ""(unknown)"", Environment)
| extend Machine = iff(isempty(Machine), ""(unknown)"", Machine)
| summarize arg_max(TimeGenerated, Version, Source) by ApplicationName, Environment, Machine;
union startup_pick, (log_pick | join kind=leftanti startup_pick on ApplicationName, Environment, Machine)
| order by ApplicationName asc, Environment asc, Machine asc";
    private readonly ConcurrentDictionary<ClientKey, LogsQueryClient> _clientCache = new();
    private readonly ITimeToLiveCache _timeToLiveCache;
    private readonly ApplicationInsightsOptions _options;
    private readonly ILogger<ApplicationInsightsService> _logger;

    private readonly record struct ClientKey(ApplicationInsightsAuthMode AuthMode, string TenantId, string ClientId);

    public ApplicationInsightsService(
        ITimeToLiveCache timeToLiveCache,
        IOptions<ApplicationInsightsOptions> options,
        ILogger<ApplicationInsightsService> logger)
    {
        _timeToLiveCache = timeToLiveCache;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<VolumeBySource> GetVolumeBySourceAsync(
        IApplicationInsightsContext context,
        TimeSpan timeSpan,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"volbysource|{context.ToKey()}|{timeSpan}";
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            // Billing-grade volume straight from the Usage table — cheap, no telemetry scan.
            const string query = @"
Usage
| where IsBillable == true
| summarize Mb = sum(Quantity) by Source = DataType
| where Mb > 0
| order by Mb desc";
            try
            {
                var client = GetClient(context);
                var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;
                var response = await client.QueryWorkspaceAsync(workspaceId, query, new LogsQueryTimeRange(timeSpan));

                var list = new List<VolumeBySource>();
                foreach (var table in response.Value.AllTables)
                {
                    var sourceIdx = GetColumnIndex(table, "Source");
                    var mbIdx = GetColumnIndex(table, "Mb");
                    foreach (var row in table.Rows)
                    {
                        var source = row[sourceIdx]?.ToString();
                        if (string.IsNullOrEmpty(source)) continue;
                        var raw = row[mbIdx];
                        if (raw is null) continue;
                        list.Add(new VolumeBySource
                        {
                            Source = source,
                            Mb = System.Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture)
                        });
                    }
                }
                return list.ToArray();
            }
            catch (Exception e)
            {
                // The Usage table may be unreadable (credential lacks workspace read). Degrade to
                // empty so the cost view shows "unavailable" rather than failing the whole page.
                _logger.LogWarning(e, "Unable to read the Usage table for volume-by-source. Returning empty.");
                return Array.Empty<VolumeBySource>();
            }
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    public async Task<CapTimeline> GetCapTimelineAsync(IApplicationInsightsContext context, int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"captimeline|{context.ToKey()}|{days}";
        return await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            try
            {
                var client = GetClient(context);
                var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

                // Configured cap: latest "Daily quota changed to N" config event (rare — search wide).
                double? capGb = null;
                var capResp = await client.QueryWorkspaceAsync(workspaceId,
                    "Operation | where Detail has 'Daily quota changed to' | top 1 by TimeGenerated desc | project Detail",
                    new LogsQueryTimeRange(TimeSpan.FromDays(365)));
                foreach (var table in capResp.Value.AllTables)
                {
                    var detailIdx = GetColumnIndex(table, "Detail");
                    foreach (var row in table.Rows)
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(row[detailIdx]?.ToString() ?? "", @"Daily quota changed to (\d+(?:\.\d+)?)");
                        if (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var g)) capGb = g;
                    }
                }

                var window = new LogsQueryTimeRange(TimeSpan.FromDays(days));

                // First cap-hit per day, from the workspace "data collection stopped" operation events.
                var hits = new Dictionary<DateTime, DateTime>();
                var hitResp = await client.QueryWorkspaceAsync(workspaceId,
                    "Operation | where OperationCategory == 'Data Collection Status' | where Detail has 'stopped due to daily limit' | summarize HitUtc = min(TimeGenerated) by Day = startofday(TimeGenerated)",
                    window);
                foreach (var table in hitResp.Value.AllTables)
                {
                    var dayIdx = GetColumnIndex(table, "Day");
                    var hitIdx = GetColumnIndex(table, "HitUtc");
                    foreach (var row in table.Rows) hits[GetDateTime(row, dayIdx)] = GetDateTime(row, hitIdx);
                }

                // Billed volume per day (GB).
                var volume = new Dictionary<DateTime, double>();
                var volResp = await client.QueryWorkspaceAsync(workspaceId,
                    "Usage | where IsBillable == true | summarize Gb = sum(Quantity) / 1024.0 by Day = startofday(TimeGenerated)",
                    window);
                foreach (var table in volResp.Value.AllTables)
                {
                    var dayIdx = GetColumnIndex(table, "Day");
                    var gbIdx = GetColumnIndex(table, "Gb");
                    foreach (var row in table.Rows)
                    {
                        var raw = row[gbIdx];
                        if (raw is null) continue;
                        volume[GetDateTime(row, dayIdx)] = System.Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                var capDays = volume.Keys.Union(hits.Keys)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .Select(day =>
                    {
                        var gb = volume.GetValueOrDefault(day);
                        DateTime? hit = hits.TryGetValue(day, out var h) ? h : null;
                        double? est = null;
                        if (hit.HasValue)
                        {
                            // Volume-at-hit ≈ the cap; extrapolate to a full day by the elapsed fraction.
                            var hours = (hit.Value - day).TotalHours;
                            var baseGb = capGb ?? gb;
                            if (hours > 0) est = baseGb * 24.0 / hours;
                        }
                        return new CapDay { DateUtc = day, IngestedGb = gb, CapHitUtc = hit, EstimatedUncappedGb = est };
                    })
                    .ToArray();

                return new CapTimeline { CapGb = capGb, Days = capDays };
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to build the ingestion-cap timeline (Operation/Usage not readable?). Returning empty.");
                return new CapTimeline { CapGb = null, Days = [] };
            }
        });
    }

    public async Task<bool> CanConnectAsync(IApplicationInsightsContext context)
    {
        var incidentId = IncidentId.New();
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;
        var tenantId = context?.TenantId ?? _options.TenantId;
        var clientId = context?.ClientId ?? _options.ClientId;
        var authMode = context?.AuthMode ?? _options.AuthMode;

        try
        {
            var client = GetClient(context);
            // Cheapest possible probe: print produces a 1x1 result without scanning any table.
            // Azure still validates token, tenant, workspace existence, and Reader access.
            _ = await client.QueryWorkspaceAsync(workspaceId, "print _ = 'ok'", new LogsQueryTimeRange(TimeSpan.FromSeconds(1)), _probeOptions);

            _logger.LogInformation(
                "AI CanConnect succeeded. Incident={IncidentId} WorkspaceId={WorkspaceId} TenantId={TenantId} ClientId={ClientId} AuthMode={AuthMode}",
                incidentId, MaskId(workspaceId), MaskId(tenantId), MaskId(clientId), authMode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI CanConnect failed. Incident={IncidentId} WorkspaceId={WorkspaceId} TenantId={TenantId} ClientId={ClientId} AuthMode={AuthMode}",
                incidentId, MaskId(workspaceId), MaskId(tenantId), MaskId(clientId), authMode);
            return false;
        }
    }

    private static string MaskId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "(none)";
        return id.Length <= 8 ? id : id.Substring(0, 8) + "…";
    }

    public async IAsyncEnumerable<EnvironmentOption> GetEnvironments(IApplicationInsightsContext context)
    {
        var items = await _timeToLiveCache.GetAsync(context.ToKey(), async () =>
        {
            var items = await GetEnvironmentsInternal(context)
                .Select(x => new EnvironmentOption(x))
                .ToArrayAsync();
            return items;
        });

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<string> GetEnvironmentsInternal(IApplicationInsightsContext context)
    {
        var lookbackDays = Debugger.IsAttached ? 3 : 30;

        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        yield return null; // Any environment

        var from = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, lookbackDays));
        var to = DateTimeOffset.UtcNow;

        var query = $@"
union
(
    AppTraces
    | extend _p = todynamic(Properties)
    | project Environment = {EnvironmentProjection}
),
(
    AppExceptions
    | extend _p = todynamic(Properties)
    | project Environment = {EnvironmentProjection}
),
(
    AppRequests
    | extend _p = todynamic(Properties)
    | project Environment = {EnvironmentProjection}
)
| extend Environment = trim(' ', Environment)
| where isnotempty(Environment)
| summarize by Environment
| order by Environment asc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(from, to));

        var table = response.Value.Table;
        var envIndex = GetColumnIndex(table, "Environment");

        foreach (var row in table.Rows)
        {
            var env = row[envIndex]?.ToString();
            if (!string.IsNullOrWhiteSpace(env))
            {
                yield return env;
            }
        }
    }

    public async IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"search|{context.ToKey()}|{environment}|{text}|{timeSpan}|{minSeverityLevel}";
        // CancellationToken.None inside the cache factory: a cancel from one caller must not poison
        // the cached load for other concurrent callers (the network call continues; the cancelling
        // caller short-circuits below at the yield-loop check).
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = await SearchInternalAsync(context, environment, text, timeSpan, minSeverityLevel).ToArrayAsync();
            return list.GroupBy(x => x.Id).Select(x => x.First()).ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private async IAsyncEnumerable<LogItem> SearchInternalAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        static string EscapeKqlSingleQuoted(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        static string StripWrappingQuotes(string value)
        {
            if (value.Length < 2)
            {
                return value;
            }

            var first = value[0];
            var last = value[^1];

            var isStraightDouble = first == '"' && last == '"';
            var isStraightSingle = first == '\'' && last == '\'';
            var isCurlyDouble = first == '“' && last == '”';
            var isCurlySingle = first == '‘' && last == '’';

            if (isStraightDouble || isStraightSingle || isCurlyDouble || isCurlySingle)
            {
                return value.Substring(1, value.Length - 2).Trim();
            }

            return value;
        }

        var envValue = string.IsNullOrWhiteSpace(environment) ? null : environment.Trim();
        var textValue = string.IsNullOrWhiteSpace(text) ? null : StripWrappingQuotes(text.Trim());

        var envFilter = envValue is null
            ? string.Empty
            : $"\n| where Environment == '{EscapeKqlSingleQuoted(envValue)}' or isempty(Environment)";

        var textFilter = textValue is null
            ? string.Empty
            : $"\n| where Message contains '{EscapeKqlSingleQuoted(textValue)}' or CorrelationId contains '{EscapeKqlSingleQuoted(textValue)}'";

        // =========================
        // AppExceptions
        // =========================
        var query = $@"
AppExceptions
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(OuterMessage)
{envFilter}
{textFilter}
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId)))
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message,
    SeverityLevel,
    Id,
    Fingerprint,
    CorrelationId,
    OperationId
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var correlationIndex = GetColumnIndex(table, "CorrelationId");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = row[idIndex]?.ToString(),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Exception,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString(),
                    CorrelationId = row[correlationIndex]?.ToString() ?? string.Empty
                };
            }
        }

        // =========================
        // AppTraces
        // =========================
        query = $@"
AppTraces
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    OriginalFormat = tostring(_p[""OriginalFormat""])
{envFilter}
{textFilter}
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource)))
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message = tostring(Message),
    SeverityLevel,
    Id,
    Fingerprint,
    CorrelationId,
    OperationId
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var correlationIndex = GetColumnIndex(table, "CorrelationId");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = row[idIndex]?.ToString(),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Trace,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString(),
                    CorrelationId = row[correlationIndex]?.ToString() ?? string.Empty
                };
            }
        }

        // =========================
        // AppRequests
        // =========================
        query = $@"
AppRequests
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(Name)
{envFilter}
{textFilter}
| extend
    SeverityLevel = iif(tobool(coalesce(Success, true)), 1, 3)
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(Message)))
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message,
    SeverityLevel,
    Id,
    Fingerprint,
    CorrelationId,
    OperationId,
    ResultCode,
    Success
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var correlationIndex = GetColumnIndex(table, "CorrelationId");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = row[idIndex]?.ToString(),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Request,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString(),
                    CorrelationId = row[correlationIndex]?.ToString() ?? string.Empty
                };
            }
        }
    }

    public async IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"measure|{context.ToKey()}|{environment}|{timeSpan}";
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = await GetMeasureInternalAsync(context, environment, timeSpan).ToArrayAsync();
            return list.GroupBy(x => x.Id).Select(x => x.First()).ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private async IAsyncEnumerable<MeasureData> GetMeasureInternalAsync(
        IApplicationInsightsContext context,
        string environment,
        TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        static string EscapeKqlSingleQuoted(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        var envValue = string.IsNullOrWhiteSpace(environment) ? null : environment.Trim();

        // If env is provided:
        //   include matching env OR rows with no env logged (empty).
        // If env is not provided:
        //   no filter (all envs).
        var envFilter = envValue is null
            ? string.Empty
            : $"\n| where isempty(Environment) or Environment =~ '{EscapeKqlSingleQuoted(envValue)}'";

        var query = $@"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Method = tostring(parse_json(tostring(_p[""Details""]))[""Method""])
| where Method == ""Measure""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = trim(' ', {EnvironmentProjection}),
    OriginalFormat = trim(' ', tostring(_p[""OriginalFormat""])),
    ElapsedRaw = extract(@""in ([0-9:\.]+) ms"", 1, tostring(Message))
{envFilter}
| extend
    FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| extend
    Elapsed = totimespan(ElapsedRaw),
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource)))
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message = tostring(Message),
    Environment,
    ApplicationName,
    Action,
    Elapsed
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var actionIndex = GetColumnIndex(table, "Action");
            var elapsedIndex = GetColumnIndex(table, "Elapsed");

            foreach (var row in table.Rows)
            {
                yield return new MeasureData
                {
                    Id = row[idIndex]?.ToString()!,
                    Fingerprint = row[fingerprintIndex]?.ToString()!,
                    TimeGenerated = GetDateTime(row, timeIndex),

                    Message = row[messageIndex]?.ToString()!,
                    Environment = row[environmentIndex]?.ToString() ?? "",
                    Application = row[applicationIndex]?.ToString() ?? "",

                    Action = row[actionIndex]?.ToString() ?? "",
                    Elapsed = GetTimeSpan(row, elapsedIndex)
                };
            }
        }
    }

    public async IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"count|{context.ToKey()}|{environment}|{timeSpan}";
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = await GetCountInternalAsync(context, environment, timeSpan).ToArrayAsync();
            return list.GroupBy(x => x.Id).Select(x => x.First()).ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private async IAsyncEnumerable<CountData> GetCountInternalAsync(
        IApplicationInsightsContext context,
        string environment,
        TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        static string EscapeKqlSingleQuoted(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        var envValue = string.IsNullOrWhiteSpace(environment) ? null : environment.Trim();

        // If env is provided:
        //   include matching env OR rows with no env logged (empty).
        // If env is not provided:
        //   no filter (all envs).
        var envFilter = envValue is null
            ? string.Empty
            : $"\n| where isempty(Environment) or Environment =~ '{EscapeKqlSingleQuoted(envValue)}'";

        var query = $@"
AppTraces
| extend _p = todynamic(Properties)
| extend
    DetailsJson = parse_json(tostring(_p[""Details""]))
| extend
    Method = tostring(DetailsJson[""Method""])
| where Method == ""Count""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = trim(' ', {EnvironmentProjection}),
    OriginalFormat = trim(' ', tostring(_p[""OriginalFormat""])),
    CountRaw = tostring(DetailsJson[""Count""])
{envFilter}
| extend
    FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource))),
    Count = toint(CountRaw)
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message = tostring(Message),
    Environment,
    ApplicationName,
    Action,
    Count
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var actionIndex = GetColumnIndex(table, "Action");
            var countIndex = GetColumnIndex(table, "Count");

            foreach (var row in table.Rows)
            {
                yield return new CountData
                {
                    Id = row[idIndex]?.ToString()!,
                    Fingerprint = row[fingerprintIndex]?.ToString()!,
                    TimeGenerated = GetDateTime(row, timeIndex),

                    Message = row[messageIndex]?.ToString()!,
                    Environment = row[environmentIndex]?.ToString() ?? "",
                    Application = row[applicationIndex]?.ToString() ?? "",

                    Action = row[actionIndex]?.ToString() ?? "",
                    Count = GetInt(row, countIndex)
                };
            }
        }
    }

    public async Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var query = source switch
        {
            LogSource.Exception => $@"
AppExceptions
| where _ItemId == ""{id}""
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Version = {VersionProjection},
    Message = tostring(OuterMessage),
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId))),
    SeverityLevel = toint(SeverityLevel),
    Raw = pack_all()
| project TimeGenerated, Message, Environment, Application, Version, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            LogSource.Trace => $@"
AppTraces
| where _ItemId == ""{id}""
| extend _p = todynamic(Properties)
| extend
    OriginalFormat = tostring(_p[""OriginalFormat""]),
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Version = {VersionProjection},
    Message = tostring(Message),
    SeverityLevel = toint(SeverityLevel),
    Raw = pack_all()
| extend
    Fingerprint = base64_encode_tostring(tostring(hash(OriginalFormat)))
| project TimeGenerated, Message, Environment, Application, Version, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            LogSource.Request => $@"
AppRequests
| where _ItemId == ""{id}""
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Version = {VersionProjection},
    Message = tostring(Name),
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    SeverityLevel = iif(tobool(Success), 1, 3),
    Raw = pack_all()
| project TimeGenerated, Message, Environment, Application, Version, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        var table = response.Value.Table;

        if (table.Rows.Count == 0)
        {
            throw new InvalidOperationException($"No {source} item found for id '{id}'.");
        }

        var row = table.Rows[0];

        var timeIndex = GetColumnIndex(table, "TimeGenerated");
        var messageIndex = GetColumnIndex(table, "Message");
        var envIndex = GetColumnIndex(table, "Environment");
        var appIndex = GetColumnIndex(table, "Application");
        var versionIndex = GetColumnIndex(table, "Version");
        var fpIndex = GetColumnIndex(table, "Fingerprint");
        var severityIndex = GetColumnIndex(table, "SeverityLevel");
        var correlationIndex = GetColumnIndex(table, "CorrelationId");
        var rawIndex = GetColumnIndex(table, "Raw");

        if (row[rawIndex] is not BinaryData binary)
        {
            throw new InvalidOperationException($"Row for {source} item id '{id}' has no Raw payload (got {row[rawIndex]?.GetType().Name ?? "null"}).");
        }
        var json = binary.ToString();

        var rawNullable = JsonSerializer.Deserialize<Dictionary<string, object>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

        var raw = new Dictionary<string, object>();
        foreach (var kvp in rawNullable)
        {
            raw[kvp.Key] = kvp.Value!;
        }

        return new LogDetails
        {
            Id = id,
            Source = source,
            SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
            CorrelationId = row[correlationIndex]?.ToString() ?? string.Empty,
            Version = row[versionIndex]?.ToString() ?? string.Empty,
            Fingerprint = row[fpIndex]?.ToString()!,
            TimeGenerated = GetDateTime(row, timeIndex),
            Message = row[messageIndex]?.ToString()!,
            Environment = row[envIndex]?.ToString()!,
            Application = row[appIndex]?.ToString()!,
            Raw = raw,
            RawJson = JsonSerializer.Serialize(rawNullable, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    public async Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan, int maxItems = 100)
    {
        if (maxItems <= 0) throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be positive.");

        var cacheKey = $"summary|{context.ToKey()}|{fingerprint}|{source}|{environment}|{timeSpan}|{maxItems}";
        return await _timeToLiveCache.GetAsync(cacheKey, () => GetSummaryInternalAsync(context, fingerprint, source, environment, timeSpan, maxItems));
    }

    private async Task<SummaryData> GetSummaryInternalAsync(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan, int maxItems)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        // Each branch builds a `_matched` set, computes _total = count() before take, then projects
        // the top maxItems with TotalCount attached as a column on every row. This lets the UI
        // tell the operator "showing X of Y" without a second query.
        var query = source switch
        {
            LogSource.Exception => $@"
let _matched = AppExceptions
| extend _p = todynamic(Properties)
| extend
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId))),
    Message = tostring(OuterMessage),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Id = _ItemId
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel;
let _total = toscalar(_matched | count);
_matched
| order by TimeGenerated desc
| take {maxItems}
| extend TotalCount = _total",

            LogSource.Trace => $@"
let _matched = AppTraces
| extend _p = todynamic(Properties)
| extend
    OriginalFormat = tostring(_p[""OriginalFormat""]),
    Message = tostring(Message),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Id = _ItemId
| extend
    FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| extend
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource)))
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel;
let _total = toscalar(_matched | count);
_matched
| order by TimeGenerated desc
| take {maxItems}
| extend TotalCount = _total",

            LogSource.Request => $@"
let _matched = AppRequests
| extend _p = todynamic(Properties)
| extend
    Message = tostring(Name),
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = iif(tobool(Success), 1, 3),
    Id = _ItemId
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel;
let _total = toscalar(_matched | count);
_matched
| order by TimeGenerated desc
| take {maxItems}
| extend TotalCount = _total",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new LogsQueryTimeRange(timeSpan));

        var table = response.Value.Table;
        if (table.Rows.Count == 0)
        {
            throw new InvalidOperationException("No items found for fingerprint.");
        }

        var idIndex = GetColumnIndex(table, "Id");
        var timeIndex = GetColumnIndex(table, "TimeGenerated");
        var messageIndex = GetColumnIndex(table, "Message");
        var envIndex = GetColumnIndex(table, "Environment");
        var appIndex = GetColumnIndex(table, "Application");
        var severityIndex = GetColumnIndex(table, "SeverityLevel");
        var totalIndex = GetColumnIndex(table, "TotalCount");

        var items = table.Rows
            .Select(row => new SummaryData.Item
            {
                Id = row[idIndex]?.ToString() ?? "",
                TimeGenerated = GetDateTime(row, timeIndex),
                Message = row[messageIndex]?.ToString() ?? ""
            })
            .Where(item => !string.IsNullOrEmpty(item.Id)) // skip rows without an Id — they can't be linked back
            .ToArray();

        var first = table.Rows[0];
        // Every row in the response carries the same TotalCount column (computed once via
        // toscalar in KQL) — read it off the first row.
        var totalCount = System.Convert.ToInt64(first[totalIndex] ?? 0L, System.Globalization.CultureInfo.InvariantCulture);

        return new SummaryData
        {
            Fingerprint = fingerprint,
            Message = first[messageIndex]?.ToString() ?? "",
            Environment = first[envIndex]?.ToString() ?? "",
            Application = first[appIndex]?.ToString() ?? "",
            SeverityLevel = (SeverityLevel)GetInt(first, severityIndex),
            Source = source,
            Items = items,
            TotalCount = totalCount
        };
    }

    public async IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"summaries|{context.ToKey()}|{environment}|{timeSpan}";
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = await GetSummariesInternal(context, environment, timeSpan).ToArrayAsync();
            return list.GroupBy(x => x.Fingerprint).Select(x => x.First()).ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private async IAsyncEnumerable<SummarySubset> GetSummariesInternal(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        static string EscapeKqlSingleQuoted(string value)
        {
            return value
                .Replace("'", "''")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        var envValue = string.IsNullOrWhiteSpace(environment) ? null : environment.Trim();

        // If env is provided: include matching env OR rows with no env (empty/null).
        // If env is not provided: no filter (all envs).
        var envFilter = envValue is null
            ? string.Empty
            : $"\n| where isempty(Environment) or Environment =~ '{EscapeKqlSingleQuoted(envValue)}'";

        var appExceptionsQuery = $@"
AppExceptions
| extend _p = todynamic(Properties)
| project
    TimeGenerated,
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId))),
    Message = tostring(OuterMessage),
    Environment = trim(' ', {EnvironmentProjection}),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = toint(SeverityLevel)
{envFilter}
{SummarizeByFingerprint}";

        var appTracesQuery = $@"
AppTraces
| extend _p = todynamic(Properties)
| extend OriginalFormat = trim(' ', tostring(_p[""OriginalFormat""]))
| extend FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| project
    TimeGenerated,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource))),
    Message = tostring(Message),
    Environment = trim(' ', {EnvironmentProjection}),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = toint(SeverityLevel)
{envFilter}
{SummarizeByFingerprint}";

        var appRequestsQuery = $@"
AppRequests
| extend _p = todynamic(Properties)
| project
    TimeGenerated,
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    Message = tostring(Name),
    Environment = trim(' ', {EnvironmentProjection}),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = iif(tobool(coalesce(Success, true)), 1, 3)
{envFilter}
{SummarizeByFingerprint}";

        async IAsyncEnumerable<SummarySubset> Run(string query, LogSource source)
        {
            var response = await client.QueryWorkspaceAsync(
                workspaceId,
                query,
                new LogsQueryTimeRange(timeSpan),
                _summariesQueryOptions);

            if (response.Value.Error != null)
            {
                _logger.LogWarning(
                    "GetSummaries returned a partial result for {Source}. Error={Error} WorkspaceId={WorkspaceId} TimeSpan={TimeSpan}",
                    source, response.Value.Error.Message, MaskId(workspaceId), timeSpan);
            }

            var table = response.Value.Table;

            var fp = GetColumnIndex(table, "Fingerprint");
            var msg = GetColumnIndex(table, "Message");
            var env = GetColumnIndex(table, "Environment");
            var app = GetColumnIndex(table, "Application");
            var sev = GetColumnIndex(table, "SeverityLevel");
            var last = GetColumnIndex(table, "LastTimeGenerated");
            var count = GetColumnIndex(table, "Count");

            foreach (var row in table.Rows)
            {
                var fingerprint = row[fp]?.ToString();
                if (string.IsNullOrEmpty(fingerprint)) continue; // can't summarize a row without a fingerprint key

                yield return new SummarySubset
                {
                    Fingerprint = fingerprint,
                    Message = row[msg]?.ToString() ?? "",
                    Environment = row[env]?.ToString() ?? "",
                    Application = row[app]?.ToString() ?? "",
                    SeverityLevel = (SeverityLevel)GetInt(row, sev),
                    Source = source,
                    LastTimeGenerated = GetDateTime(row, last),
                    Count = GetInt(row, count)
                };
            }
        }

        await foreach (var x in Run(appExceptionsQuery, LogSource.Exception))
        {
            yield return x;
        }

        await foreach (var x in Run(appTracesQuery, LogSource.Trace))
        {
            yield return x;
        }

        await foreach (var x in Run(appRequestsQuery, LogSource.Request))
        {
            yield return x;
        }
    }

    public async IAsyncEnumerable<VersionMatrixCell> GetVersionMatrixAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, bool forceRefresh = false)
    {
        // Cache key bumped to v3 when the Machine column was added — without that suffix, a hot-
        // reloaded process would keep serving pre-Machine cell snapshots from the TtlCache (which
        // doesn't track schema migrations) and every row would read "(unknown)" forever.
        var cacheKey = $"versionmatrix-v3|{context.ToKey()}|{lookback}";

        // forceRefresh wired up to the UI's Refresh button (via VersionMatrixService.RefreshAsync):
        // drops the TtlCache entry first so the next GetAsync actually re-runs the KQL. Without
        // this, RefreshAsync only cleared its own in-memory view dict — the underlying cells
        // stayed cached for an hour, so users couldn't force a fresh fetch even by clicking
        // Refresh after a code change.
        if (forceRefresh)
        {
            await _timeToLiveCache.InvalidateAsync<VersionMatrixCell[]>(cacheKey);
        }

        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            return await GetVersionMatrixInternalAsync(context, lookback).ToArrayAsync();
        });

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<VersionMatrixCell> GetVersionMatrixInternalAsync(IApplicationInsightsContext context, TimeSpan? lookback)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var query = $@"
let startup = AppTraces
| extend _p = todynamic(Properties)
| where tostring(_p[""Quilt4NetStartup""]) == ""true""
| extend
    ApplicationName = tostring(_p[""ApplicationName""]),
    Environment = tostring(_p[""Environment""]),
    Version = tostring(_p[""Version""]),
    Machine = coalesce(
        tostring(_p[""MachineName""]),
        tostring(_p[""machineName""]),
        tostring(_p[""machine_name""]),
        tostring(_p[""host.name""]),
        tostring(_p[""ServerName""]),
        tostring(_p[""serverName""]),
        tostring(AppRoleInstance))
| where isnotempty(ApplicationName) and isnotempty(Version)
| project TimeGenerated, ApplicationName, Environment, Version, Machine, Source = ""Startup"";
let fallback = union AppTraces, AppExceptions, AppRequests
| extend _p = todynamic(Properties)
| extend
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = {EnvironmentProjection},
    Version = coalesce(tostring(_p[""application_Version""]), tostring(_p[""service.version""]), tostring(AppVersion)),
    Machine = coalesce(
        tostring(_p[""MachineName""]),
        tostring(_p[""machineName""]),
        tostring(_p[""machine_name""]),
        tostring(_p[""host.name""]),
        tostring(_p[""ServerName""]),
        tostring(_p[""serverName""]),
        tostring(AppRoleInstance))
| where isnotempty(ApplicationName) and isnotempty(Version)
| project TimeGenerated, ApplicationName, Environment, Version, Machine, Source = ""Log"";
{VersionMatrixPickStartupPreferred}";

        var range = lookback.HasValue
            ? new LogsQueryTimeRange(DateTimeOffset.UtcNow - lookback.Value, DateTimeOffset.UtcNow)
            : LogsQueryTimeRange.All;

        var response = await client.QueryWorkspaceAsync(workspaceId, query, range);
        var table = response.Value.Table;

        var appIndex = GetColumnIndex(table, "ApplicationName");
        var envIndex = GetColumnIndex(table, "Environment");
        var versionIndex = GetColumnIndex(table, "Version");
        var timeIndex = GetColumnIndex(table, "TimeGenerated");
        var sourceIndex = GetColumnIndex(table, "Source");
        var machineIndex = GetColumnIndex(table, "Machine");

        foreach (var row in table.Rows)
        {
            yield return new VersionMatrixCell
            {
                ApplicationName = row[appIndex]?.ToString() ?? "",
                Environment = row[envIndex]?.ToString() ?? "(unknown)",
                Version = row[versionIndex]?.ToString() ?? "",
                LastSeen = GetDateTime(row, timeIndex),
                Source = string.Equals(row[sourceIndex]?.ToString(), "Startup", StringComparison.Ordinal)
                    ? VersionMatrixSource.Startup
                    : VersionMatrixSource.Log,
                Machine = row[machineIndex]?.ToString() ?? ""
            };
        }
    }

    public IAsyncEnumerable<LogItem> SearchByIncidentIdAsync(IApplicationInsightsContext context, string incidentId, TimeSpan timeSpan)
    {
        var predicate = $"tostring(_p['IncidentId']) == '{EscapeKqlSingleQuoted(incidentId)}'";
        return SearchByIdAsync(context, predicate, timeSpan);
    }

    public IAsyncEnumerable<LogItem> SearchByCorrelationIdAsync(IApplicationInsightsContext context, string correlationId, TimeSpan timeSpan)
    {
        var escaped = EscapeKqlSingleQuoted(correlationId);
        var predicate =
            $"OperationId == '{escaped}' or " +
            $"tostring(_p['CorrelationId']) == '{escaped}' or " +
            $"tostring(_p['RequestId']) == '{escaped}'";
        return SearchByIdAsync(context, predicate, timeSpan);
    }

    private async IAsyncEnumerable<LogItem> SearchByIdAsync(IApplicationInsightsContext context, string wherePredicate, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var query = $@"
union withsource=_Source AppTraces, AppExceptions, AppRequests
| extend _p = todynamic(Properties)
| where {wherePredicate}
| extend
    Environment = {EnvironmentProjection},
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = coalesce(tostring(Message), tostring(OuterMessage), tostring(Name)),
    FingerprintSource = coalesce(tostring(ProblemId), tostring(_p[""OriginalFormat""]), tostring(Message), tostring(Name)),
    EffectiveSeverity = iif(_Source == ""AppRequests"", iif(tobool(coalesce(Success, true)), 1, 3), toint(SeverityLevel))
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource)))
| project TimeGenerated, _Source, Application, Environment, Message, EffectiveSeverity, Id, Fingerprint
| order by TimeGenerated asc";

        var response = await client.QueryWorkspaceAsync(workspaceId, query, new LogsQueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var sourceColIndex = GetColumnIndex(table, "_Source");
            var appIndex = GetColumnIndex(table, "Application");
            var envIndex = GetColumnIndex(table, "Environment");
            var messageIndex = GetColumnIndex(table, "Message");
            var severityIndex = GetColumnIndex(table, "EffectiveSeverity");
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");

            foreach (var row in table.Rows)
            {
                var sourceTable = row[sourceColIndex]?.ToString();
                var source = sourceTable switch
                {
                    "AppExceptions" => LogSource.Exception,
                    "AppRequests" => LogSource.Request,
                    _ => LogSource.Trace
                };

                yield return new LogItem
                {
                    Id = row[idIndex]?.ToString(),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = source,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[envIndex]?.ToString(),
                    Application = row[appIndex]?.ToString()
                };
            }
        }
    }

    // All four metric queries target the workspace-based AppMetrics table (the classic
    // customMetrics table doesn't exist on Log Analytics workspaces). Per-row "average value"
    // is computed from Sum/ItemCount because AppMetrics doesn't carry a Value column — Sum/Min/
    // Max/ItemCount are the pre-aggregated columns the OpenTelemetry → Azure Monitor exporter
    // writes. For aggregating across multiple rows we use sum(Sum)/sum(ItemCount), the weighted
    // average that matches what classic `avg(value)` produced.

    // Group by host so each machine renders as its own line. Coalesce probes Properties['MachineName']
    // first (what the Quilt4Net SDK enriches every record with — `Environment.MachineName`), then
    // Properties['host.name'] (the OpenTelemetry resource attribute), and finally AppRoleInstance
    // (Azure Monitor's per-instance id) as a last-resort fallback.
    //
    // `where isnotempty(host)` drops rows with no host identity at all. The k3s nodes run a
    // hostmetrics DaemonSet that emits system.* WITHOUT host.name/AppRole*, so without this filter
    // every node's same-named device (e.g. /dev/sda1) collapses into one blank-prefixed series and
    // averages unrelated machines together. Those cluster nodes are charted per-node via the
    // k8s.node.* methods instead (GetClusterNode*). Identified hosts (e.g. Eplicta1-7) are unaffected.

    public IAsyncEnumerable<MetricSample> GetCpuUtilizationAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'system.cpu.utilization'
| where tostring(Properties.state) == 'idle'
| extend host = coalesce(tostring(Properties['MachineName']), tostring(Properties['host.name']), AppRoleInstance)
| where isnotempty(host)
| summarize avg_idle = sum(Sum) / todouble(sum(ItemCount)) by host, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend cpu_busy_pct = 100.0 * (1 - avg_idle)
| project Series = host, Timestamp = TimeGenerated, Value = cpu_busy_pct";
        return RunMetricQueryAsync(context, query, timeSpan, $"cpu|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetMemoryUtilizationAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'system.memory.utilization'
| where tostring(Properties.state) == 'used'
| extend host = coalesce(tostring(Properties['MachineName']), tostring(Properties['host.name']), AppRoleInstance)
| where isnotempty(host)
| summarize mem_used_pct = 100.0 * sum(Sum) / todouble(sum(ItemCount)) by host, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| project Series = host, Timestamp = TimeGenerated, Value = mem_used_pct";
        return RunMetricQueryAsync(context, query, timeSpan, $"mem|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetDiskFreeAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Disk is binned a step coarser than CPU/mem because the underlying signal moves slowly
        // and we'd otherwise duplicate a near-constant value across the chart. Series remains
        // host+volume so a machine with several filesystems still renders as separate lines.
        // state=free (not used) — operators care about "how much room is left" and a line falling
        // toward zero is the natural visual signal that the disk is filling up.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        if (bin < TimeSpan.FromMinutes(5)) bin = TimeSpan.FromMinutes(5);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'system.filesystem.usage'
| where tostring(Properties.state) == 'free'
| extend host = coalesce(tostring(Properties['MachineName']), tostring(Properties['host.name']), AppRoleInstance)
| where isnotempty(host)
| extend volume = strcat(host, ' ', tostring(Properties['device']))
| summarize free_gb = (sum(Sum) / todouble(sum(ItemCount))) / 1073741824.0 by volume, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| project Series = volume, Timestamp = TimeGenerated, Value = free_gb";
        return RunMetricQueryAsync(context, query, timeSpan, $"diskfree|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public async IAsyncEnumerable<DiskCapacity> GetDiskCapacityAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Capacity is constant on a healthy filesystem (it only moves when a volume is resized),
        // so we don't bin by time — we average each state's GB across the window for one row per
        // volume. free + used + reserved = total. The window is still passed so a freshly-added
        // volume that only has data for the last 5 minutes still shows up.
        var cacheKey = $"diskcapacity|{context.ToKey()}|{timeSpan}";
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'system.filesystem.usage'
| extend host = coalesce(tostring(Properties['MachineName']), tostring(Properties['host.name']), AppRoleInstance)
| where isnotempty(host)
| extend volume = strcat(host, ' ', tostring(Properties['device']))
| extend state = tostring(Properties.state)
| summarize gb = (sum(Sum) / todouble(sum(ItemCount))) / 1073741824.0 by volume, state
| extend free_gb = iif(state == 'free', gb, 0.0)
| extend used_gb = iif(state == 'used', gb, 0.0)
| extend reserved_gb = iif(state == 'reserved', gb, 0.0)
| summarize FreeGb = sum(free_gb), UsedGb = sum(used_gb), ReservedGb = sum(reserved_gb) by volume
| extend TotalGb = FreeGb + UsedGb + ReservedGb
| project Series = volume, FreeGb, UsedGb, ReservedGb, TotalGb";

        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = new List<DiskCapacity>();
            var client = GetClient(context);
            var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;
            var response = await client.QueryWorkspaceAsync(workspaceId, query, new LogsQueryTimeRange(timeSpan));
            foreach (var table in response.Value.AllTables)
            {
                var seriesIdx = GetColumnIndex(table, "Series");
                var freeIdx = GetColumnIndex(table, "FreeGb");
                var usedIdx = GetColumnIndex(table, "UsedGb");
                var reservedIdx = GetColumnIndex(table, "ReservedGb");
                var totalIdx = GetColumnIndex(table, "TotalGb");
                foreach (var row in table.Rows)
                {
                    list.Add(new DiskCapacity(
                        row[seriesIdx]?.ToString() ?? "",
                        ToDouble(row[freeIdx]),
                        ToDouble(row[usedIdx]),
                        ToDouble(row[reservedIdx]),
                        ToDouble(row[totalIdx])));
                }
            }
            return list.ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static double ToDouble(object raw)
        => raw is null ? 0.0 : System.Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);

    public IAsyncEnumerable<MetricSample> GetNetworkThroughputAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // system.network.io is a counter — bytes since process start. Take sum(Sum) per bin so
        // the prev()-delta below measures the host's progress across bins; negative deltas
        // (process restart / counter reset) are filtered out so the chart doesn't draw a dip.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'system.network.io'
| extend host = coalesce(tostring(Properties['MachineName']), tostring(Properties['host.name']), AppRoleInstance)
| where isnotempty(host)
| summarize total_bytes = sum(Sum) by host, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| order by host asc, TimeGenerated asc
| extend prev_bytes = prev(total_bytes), prev_host = prev(host), prev_t = prev(TimeGenerated)
| where host == prev_host
| extend mb_per_sec = (total_bytes - prev_bytes) / 1048576.0 / datetime_diff('second', TimeGenerated, prev_t)
| where mb_per_sec >= 0
| project Series = host, Timestamp = TimeGenerated, Value = mb_per_sec";
        return RunMetricQueryAsync(context, query, timeSpan, $"net|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    // Cluster (kubeletstats) metrics are grouped by k8s.node.name — these come from the k3s nodes
    // (e.g. cog-audry / vm-ygg-cp-1), a separate telemetry family from the host system.* metrics
    // above. Per-row "average value" uses the same sum(Sum)/sum(ItemCount) weighted average.

    public IAsyncEnumerable<MetricSample> GetClusterNodeCpuAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Node CPU used %, = 100 * k8s.node.cpu.usage / k8s.node.allocatable_cpu (cores / total
        // schedulable cores). Both averaged per (node, bin) first, then combined. Only bins where
        // allocatable is present render — i.e. from when the collector started emitting it.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.cpu.usage', 'k8s.node.allocatable_cpu')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.cpu.usage', val, 0.0), cap = iif(Name == 'k8s.node.allocatable_cpu', val, 0.0)
| summarize used = sum(used), cap = sum(cap) by node, ts
| where cap > 0
| project Series = node, Timestamp = ts, Value = 100.0 * used / cap";
        return RunMetricQueryAsync(context, query, timeSpan, $"nodecpupct|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterNodeMemoryAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // No node memory-capacity metric, but usage + available sum to total, so memory-used %
        // is 100 * usage / (usage + available). Both metrics are averaged per (node, bin) first,
        // then combined. Series = node name.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.memory.usage', 'k8s.node.memory.available')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.memory.usage', val, 0.0), avail = iif(Name == 'k8s.node.memory.available', val, 0.0)
| summarize used = sum(used), avail = sum(avail) by node, ts
| where used + avail > 0
| project Series = node, Timestamp = ts, Value = 100.0 * used / (used + avail)";
        return RunMetricQueryAsync(context, query, timeSpan, $"nodemem|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterPodCpuAsync(IApplicationInsightsContext context, string node, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Drill-down for one node: pods scheduled on it, CPU in cores. Series = namespace/pod.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var nodeLiteral = EscapeKqlSingleQuoted(node);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'k8s.pod.cpu.usage'
| where tostring(Properties['k8s.node.name']) == '{nodeLiteral}'
| extend pod = strcat(tostring(Properties['k8s.namespace.name']), '/', tostring(Properties['k8s.pod.name']))
| where isnotempty(pod)
| summarize cores = sum(Sum) / todouble(sum(ItemCount)) by pod, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| project Series = pod, Timestamp = TimeGenerated, Value = cores";
        return RunMetricQueryAsync(context, query, timeSpan, $"podcpu|{context.ToKey()}|{node}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterPodMemoryAsync(IApplicationInsightsContext context, string node, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Drill-down for one node: pods scheduled on it, memory in MB (absolute — pods have no
        // consistent capacity to percentage against). Series = namespace/pod.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var nodeLiteral = EscapeKqlSingleQuoted(node);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name == 'k8s.pod.memory.usage'
| where tostring(Properties['k8s.node.name']) == '{nodeLiteral}'
| extend pod = strcat(tostring(Properties['k8s.namespace.name']), '/', tostring(Properties['k8s.pod.name']))
| where isnotempty(pod)
| summarize mb = (sum(Sum) / todouble(sum(ItemCount))) / 1048576.0 by pod, bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| project Series = pod, Timestamp = TimeGenerated, Value = mb";
        return RunMetricQueryAsync(context, query, timeSpan, $"podmem|{context.ToKey()}|{node}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterNodeFilesystemAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // usage / capacity per node → filesystem-used %. Both averaged per (node, bin) then combined.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.filesystem.usage', 'k8s.node.filesystem.capacity')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.filesystem.usage', val, 0.0), cap = iif(Name == 'k8s.node.filesystem.capacity', val, 0.0)
| summarize used = sum(used), cap = sum(cap) by node, ts
| where cap > 0
| project Series = node, Timestamp = ts, Value = 100.0 * used / cap";
        return RunMetricQueryAsync(context, query, timeSpan, $"nodefs|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    // Whole-cluster aggregates: average each node per bin first, THEN combine across nodes — so the
    // result is a real capacity-weighted total (a 12-core / large-RAM node contributes more than a
    // small one), not a mean of per-node ratios. One "Cluster" series.

    public IAsyncEnumerable<MetricSample> GetClusterTotalCpuAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Whole-cluster CPU used %, capacity-weighted: 100 * Σ usage / Σ allocatable_cpu across all
        // nodes per bin (a 12-core node weighs 6× a 2-core one). Only bins with allocatable render.
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.cpu.usage', 'k8s.node.allocatable_cpu')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.cpu.usage', val, 0.0), cap = iif(Name == 'k8s.node.allocatable_cpu', val, 0.0)
| summarize used = sum(used), cap = sum(cap) by node, ts
| summarize tUsed = sum(used), tCap = sum(cap) by ts
| where tCap > 0
| project Series = 'Cluster', Timestamp = ts, Value = 100.0 * tUsed / tCap";
        return RunMetricQueryAsync(context, query, timeSpan, $"clustercpupct|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterTotalMemoryAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.memory.usage', 'k8s.node.memory.available')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.memory.usage', val, 0.0), avail = iif(Name == 'k8s.node.memory.available', val, 0.0)
| summarize used = sum(used), avail = sum(avail) by node, ts
| summarize tUsed = sum(used), tAvail = sum(avail) by ts
| where tUsed + tAvail > 0
| project Series = 'Cluster', Timestamp = ts, Value = 100.0 * tUsed / (tUsed + tAvail)";
        return RunMetricQueryAsync(context, query, timeSpan, $"clustermem|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public IAsyncEnumerable<MetricSample> GetClusterTotalFilesystemAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var bin = MetricsBinSelector.PickBin(timeSpan);
        var query = $@"
AppMetrics
| where TimeGenerated > ago({MetricsBinSelector.ToKqlLiteral(timeSpan)})
| where Name in ('k8s.node.filesystem.usage', 'k8s.node.filesystem.capacity')
| extend node = tostring(Properties['k8s.node.name'])
| where isnotempty(node)
| summarize val = sum(Sum) / todouble(sum(ItemCount)) by node, Name, ts = bin(TimeGenerated, {MetricsBinSelector.ToKqlLiteral(bin)})
| extend used = iif(Name == 'k8s.node.filesystem.usage', val, 0.0), cap = iif(Name == 'k8s.node.filesystem.capacity', val, 0.0)
| summarize used = sum(used), cap = sum(cap) by node, ts
| summarize tUsed = sum(used), tCap = sum(cap) by ts
| where tCap > 0
| project Series = 'Cluster', Timestamp = ts, Value = 100.0 * tUsed / tCap";
        return RunMetricQueryAsync(context, query, timeSpan, $"clusterfs|{context.ToKey()}|{timeSpan}", cancellationToken);
    }

    public async IAsyncEnumerable<LogCountByServiceCell> GetLogCountByServiceAsync(IApplicationInsightsContext context, TimeSpan timeSpan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Cache key versioned so schema changes don't serve stale cells from the TtlCache after a
        // hot reload (Machine column → v2; Bytes + extra severity-bearing sources → v3).
        var cacheKey = $"logcount-v4|{context.ToKey()}|{timeSpan}";
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = new List<LogCountByServiceCell>();
            var client = GetClient(context);
            var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

            // Group by every dimension the admin UI may filter on (env + source) so the same
            // result set powers every subsequent filter combination without another KQL run.
            // Bytes = sum(_BilledSize) lets the view toggle count ↔ ingested volume locally.
            // Unified severity:
            //   - AppTraces: own SeverityLevel (Verbose..Critical = 0..4)
            //   - AppExceptions: always Error (3) — no useful gradient on raw exceptions
            //   - AppRequests / AppDependencies: Information (1) on success, Error (3) on failure
            //   - AppEvents / AppPageViews: Information (1) — no severity/success on the row
            var query = $@"
union withsource=_Source AppTraces, AppExceptions, AppRequests, AppDependencies, AppEvents, AppPageViews
| extend _p = todynamic(Properties)
| extend
    Service = coalesce(tostring(_p['ApplicationName']), tostring(AppRoleName), 'unknown'),
    Environment = trim(' ', {EnvironmentProjection}),
    Machine = coalesce(
        tostring(_p['MachineName']),
        tostring(_p['machineName']),
        tostring(_p['machine_name']),
        tostring(_p['host.name']),
        tostring(AppRoleInstance),
        'unknown'),
    EffectiveSeverity = case(
        _Source == 'AppExceptions', 3,
        _Source == 'AppRequests', iif(tobool(coalesce(Success, true)), 1, 3),
        _Source == 'AppDependencies', iif(tobool(coalesce(Success, true)), 1, 3),
        _Source == 'AppEvents', 1,
        _Source == 'AppPageViews', 1,
        toint(SeverityLevel))
| extend _w = coalesce(toint(ItemCount), 1)
| summarize Count = count(), Bytes = sum(_BilledSize), TrueCount = sum(_w), TrueBytes = sum(_BilledSize * _w) by Service, EffectiveSeverity, Environment, _Source, Machine
| project Service, Severity = EffectiveSeverity, Environment, Source = _Source, Count, Bytes, Machine, TrueCount, TrueBytes";

            var response = await client.QueryWorkspaceAsync(workspaceId, query, new LogsQueryTimeRange(timeSpan));
            foreach (var table in response.Value.AllTables)
            {
                var serviceIdx = GetColumnIndex(table, "Service");
                var sevIdx = GetColumnIndex(table, "Severity");
                var envIdx = GetColumnIndex(table, "Environment");
                var sourceIdx = GetColumnIndex(table, "Source");
                var countIdx = GetColumnIndex(table, "Count");
                var bytesIdx = GetColumnIndex(table, "Bytes");
                var machineIdx = GetColumnIndex(table, "Machine");
                var trueCountIdx = GetColumnIndex(table, "TrueCount");
                var trueBytesIdx = GetColumnIndex(table, "TrueBytes");
                foreach (var row in table.Rows)
                {
                    var service = row[serviceIdx]?.ToString() ?? "unknown";
                    var severity = (SeverityLevel)GetInt(row, sevIdx);
                    var environment = row[envIdx]?.ToString() ?? "";
                    var source = (row[sourceIdx]?.ToString()) switch
                    {
                        "AppExceptions" => LogSource.Exception,
                        "AppRequests" => LogSource.Request,
                        "AppDependencies" => LogSource.Dependency,
                        "AppEvents" => LogSource.Event,
                        "AppPageViews" => LogSource.PageView,
                        _ => LogSource.Trace,
                    };
                    var count = System.Convert.ToInt64(row[countIdx] ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
                    var bytes = System.Convert.ToInt64(row[bytesIdx] ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
                    var machine = row[machineIdx]?.ToString() ?? "";
                    var trueCount = System.Convert.ToInt64(row[trueCountIdx] ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
                    var trueBytes = System.Convert.ToInt64(row[trueBytesIdx] ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
                    list.Add(new LogCountByServiceCell(service, severity, environment, source, count, bytes, machine, trueCount, trueBytes));
                }
            }
            return list.ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    /// <summary>
    /// Shared executor for the four metric queries. Each query is required to project columns
    /// named exactly <c>Series</c> / <c>Timestamp</c> / <c>Value</c> so the row mapping below is
    /// reusable. Results are cached with the same TTL as other AI queries.
    /// </summary>
    private async IAsyncEnumerable<MetricSample> RunMetricQueryAsync(
        IApplicationInsightsContext context,
        string query,
        TimeSpan timeSpan,
        string cacheKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var items = await _timeToLiveCache.GetAsync(cacheKey, async () =>
        {
            var list = new List<MetricSample>();
            var client = GetClient(context);
            var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;
            var response = await client.QueryWorkspaceAsync(workspaceId, query, new LogsQueryTimeRange(timeSpan));
            foreach (var table in response.Value.AllTables)
            {
                var seriesIdx = GetColumnIndex(table, "Series");
                var tsIdx = GetColumnIndex(table, "Timestamp");
                var valIdx = GetColumnIndex(table, "Value");
                foreach (var row in table.Rows)
                {
                    var raw = row[valIdx];
                    if (raw is null) continue;
                    var value = System.Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
                    list.Add(new MetricSample(
                        row[seriesIdx]?.ToString() ?? "",
                        GetDateTime(row, tsIdx),
                        value));
                }
            }
            return list.ToArray();
        });
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static string EscapeKqlSingleQuoted(string value)
    {
        if (value is null) return string.Empty;
        return value
            .Replace("'", "''")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    internal LogsQueryClient GetClient(IApplicationInsightsContext context = null)
    {
        if (context.IsCurrent()) context = null;

        var authMode = context?.AuthMode ?? _options.AuthMode;
        var clientSecret = context?.ClientSecret ?? _options.ClientSecret;
        var tenantId = context?.TenantId ?? _options.TenantId;
        var clientId = context?.ClientId ?? _options.ClientId;

        // Cache by (authMode, tenantId, clientId). The cached LogsQueryClient wraps a cached
        // TokenCredential whose bearer token is reused across calls — saves the AAD token
        // exchange (~150 ms) on every call after the first per credential set.
        var key = new ClientKey(authMode, tenantId ?? string.Empty, clientId ?? string.Empty);
        return _clientCache.GetOrAdd(key, _ =>
        {
            var credential = CredentialFactory.Create(authMode, tenantId, clientId, clientSecret);
            return new LogsQueryClient(credential);
        });
    }

    private static int GetColumnIndex(LogsTable table, string name)
    {
        for (var i = 0; i < table.Columns.Count; i++)
        {
            if (string.Equals(table.Columns[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Column '{name}' not found.");
    }

    private static DateTime GetDateTime(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is DateTimeOffset dto)
        {
            return dto.UtcDateTime;
        }

        if (value is DateTime dt)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        var s = value?.ToString();
        return string.IsNullOrEmpty(s) ? default : DateTime.Parse(s);
    }

    private static int GetInt(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return (int)l;
        }

        var s = value?.ToString();
        return string.IsNullOrEmpty(s) ? 0 : int.Parse(s);
    }

    private static TimeSpan GetTimeSpan(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is TimeSpan ts)
        {
            return ts;
        }

        return TimeSpan.Parse(value?.ToString() ?? "00:00:00");
    }
}