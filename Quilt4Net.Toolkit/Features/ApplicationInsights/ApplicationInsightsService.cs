using System.Diagnostics;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tharga.Cache;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsService : IApplicationInsightsService
{
    private readonly ITimeToLiveCache _timeToLiveCache;
    private readonly ApplicationInsightsOptions _options;

    public ApplicationInsightsService(ITimeToLiveCache timeToLiveCache, IOptions<ApplicationInsightsOptions> options)
    {
        _timeToLiveCache = timeToLiveCache;
        _options = options.Value;
    }

    public async Task<bool> CanConnectAsync(IApplicationInsightsContext context)
    {
        try
        {
            var client = GetClient(context);
            var detailQuery = "AppTraces";
            _ = await client.QueryWorkspaceAsync(context.WorkspaceId, detailQuery, new QueryTimeRange(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now));

            return true;
        }
        catch (Exception)
        {
            return false;
        }
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

        var query = @"
union
(
    AppTraces
    | extend _p = todynamic(Properties)
    | project Environment = tostring(_p[""AspNetCoreEnvironment""])
),
(
    AppExceptions
    | extend _p = todynamic(Properties)
    | project Environment = tostring(_p[""AspNetCoreEnvironment""])
),
(
    AppRequests
    | extend _p = todynamic(Properties)
    | project Environment = tostring(_p[""AspNetCoreEnvironment""])
)
| extend Environment = trim(' ', Environment)
| where isnotempty(Environment)
| summarize by Environment
| order by Environment asc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(from, to));

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

    public async IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose)
    {
        //TODO: Cache here...
        var items = await SearchInternalAsync(context, environment, text, timeSpan, minSeverityLevel).ToArrayAsync();
        foreach (var item in items.GroupBy(x => x.Id).Select(x => x.First()))
        {
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

        var textFilterExceptionsAndTraces = textValue is null
            ? string.Empty
            : $"\n| where Message contains '{EscapeKqlSingleQuoted(textValue)}'";

        var textFilterRequests = textValue is null
            ? string.Empty
            : $"\n| where Message contains '{EscapeKqlSingleQuoted(textValue)}' or CorrelationId contains '{EscapeKqlSingleQuoted(textValue)}'";

        // =========================
        // AppExceptions
        // =========================
        var query = $@"
AppExceptions
| extend _p = todynamic(Properties)
| extend
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(OuterMessage)
{envFilter}
{textFilterExceptionsAndTraces}
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
    OperationId
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

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
                    Application = row[applicationIndex]?.ToString()
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
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    OriginalFormat = tostring(_p[""OriginalFormat""])
{envFilter}
{textFilterExceptionsAndTraces}
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
    OperationId
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

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
                    Application = row[applicationIndex]?.ToString()
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
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(Name)
{envFilter}
{textFilterRequests}
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
    OperationId,
    ResultCode,
    Success
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

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
                    Application = row[applicationIndex]?.ToString()
                };
            }
        }
    }

    public async IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        //TODO: Cache here...
        var items = await GetMeasureInternalAsync(context, environment, timeSpan).ToArrayAsync();
        foreach (var item in items.GroupBy(x => x.Id).Select(x => x.First()))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<MeasureData> GetMeasureInternalAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        //var envFilter = environment ?? string.Empty;

        var query = @$"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Method = tostring(parse_json(tostring(_p[""Details""]))[""Method""])
| where Method == ""Measure""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    OriginalFormat = tostring(_p[""OriginalFormat""]),
    ElapsedRaw = extract(@""in ([0-9:\.]+) ms"", 1, Message)
| extend
    Elapsed = totimespan(ElapsedRaw),
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(OriginalFormat)))
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message,
    Environment,
    ApplicationName,
    Action,
    Elapsed
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

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
                    Environment = row[environmentIndex]?.ToString()!,
                    Application = row[applicationIndex]?.ToString()!,

                    Action = row[actionIndex]?.ToString()!,
                    Elapsed = GetTimeSpan(row, elapsedIndex)
                };
            }
        }
    }

    public async IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        //TODO: Cache here...
        var items = await GetCountInternalAsync(context, environment, timeSpan).ToArrayAsync();
        foreach (var item in items.GroupBy(x => x.Id).Select(x => x.First()))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<CountData> GetCountInternalAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var envFilter = environment ?? string.Empty;

        var query = @$"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Method = tostring(parse_json(tostring(_p[""Details""]))[""Method""])
| where Method == ""Count""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    OriginalFormat = tostring(_p[""OriginalFormat""]),
    Count = tostring(parse_json(tostring(_p[""Details""]))[""Count""])
| extend
    Id = _ItemId,
    Fingerprint = base64_encode_tostring(tostring(hash(OriginalFormat)))
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message,
    Environment,
    ApplicationName,
    Action,
    Count
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

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
                    Environment = row[environmentIndex]?.ToString()!,
                    Application = row[applicationIndex]?.ToString()!,

                    Action = row[actionIndex]?.ToString()!,
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
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(OuterMessage),
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId))),
    SeverityLevel = toint(SeverityLevel),
    Raw = pack_all()
| project TimeGenerated, Message, Environment, Application, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            LogSource.Trace => $@"
AppTraces
| where _ItemId == ""{id}""
| extend _p = todynamic(Properties)
| extend OriginalFormat = tostring(_p[""OriginalFormat""])
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(Message),
    Fingerprint = base64_encode_tostring(tostring(hash(OriginalFormat))),
    SeverityLevel = toint(SeverityLevel),
    Raw = pack_all()
| project TimeGenerated, Message, Environment, Application, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            LogSource.Request => $@"
AppRequests
| where _ItemId == ""{id}""
| extend _p = todynamic(Properties)
| extend
    CorrelationId = tostring(_p[""CorrelationId""]),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Message = tostring(Name),
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    SeverityLevel = iif(tobool(Success), 1, 3),
    Raw = pack_all()
| project TimeGenerated, Message, Environment, Application, Fingerprint, SeverityLevel, CorrelationId, Raw
| take 1",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

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
        var fpIndex = GetColumnIndex(table, "Fingerprint");
        var severityIndex = GetColumnIndex(table, "SeverityLevel");
        var correlationIndex = GetColumnIndex(table, "CorrelationId");
        var rawIndex = GetColumnIndex(table, "Raw");

        var binary = (BinaryData)row[rawIndex]!;
        var json = binary.ToString();

        var rawNullable = JsonSerializer.Deserialize<Dictionary<string, object?>>(
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
            Fingerprint = row[fpIndex]?.ToString()!,
            TimeGenerated = GetDateTime(row, timeIndex),
            Message = row[messageIndex]?.ToString()!,
            Environment = row[envIndex]?.ToString()!,
            Application = row[appIndex]?.ToString()!,
            Raw = raw,
            RawJson = JsonSerializer.Serialize(rawNullable, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    public async Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan)
    {
        //TODO: Cache here...
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var query = source switch
        {
            LogSource.Exception => $@"
AppExceptions
| extend _p = todynamic(Properties)
| extend
    Fingerprint = base64_encode_tostring(tostring(hash(ProblemId))),
    Message = tostring(OuterMessage),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Id = _ItemId
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel
| order by TimeGenerated desc",

            LogSource.Trace => $@"
AppTraces
| extend _p = todynamic(Properties)
| extend
    OriginalFormat = tostring(_p[""OriginalFormat""]),
    Fingerprint = base64_encode_tostring(tostring(hash(OriginalFormat))),
    Message = tostring(Message),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Id = _ItemId
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel
| order by TimeGenerated desc",

            LogSource.Request => $@"
AppRequests
| extend _p = todynamic(Properties)
| extend
    Message = tostring(Name),
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = iif(tobool(Success), 1, 3),
    Id = _ItemId
| where Fingerprint == ""{fingerprint}""
| project Id, TimeGenerated, Message, Environment, Application, SeverityLevel
| order by TimeGenerated desc",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

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

        var items = table.Rows.Select(row => new SummaryData.Item
        {
            Id = row[idIndex]!.ToString()!,
            TimeGenerated = GetDateTime(row, timeIndex),
            Message = row[messageIndex]!.ToString()!
        }).ToArray();

        var first = table.Rows[0];

        return new SummaryData
        {
            Fingerprint = fingerprint,
            Message = first[messageIndex]!.ToString()!,
            Environment = first[envIndex]!.ToString()!,
            Application = first[appIndex]!.ToString()!,
            SeverityLevel = (SeverityLevel)GetInt(first, severityIndex),
            Source = source,
            Items = items
        };
    }

    public async IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        //TODO: Cache here...
        var items = await GetSummariesInternal(context, environment, timeSpan).ToArrayAsync();
        foreach (var item in items.GroupBy(x => x.Fingerprint).Select(x => x.First()))
        {
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
    Environment = trim(' ', tostring(_p[""AspNetCoreEnvironment""])),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = toint(SeverityLevel)
{envFilter}
| summarize
    Count = count(),
    LastTimeGenerated = max(TimeGenerated)
    by Fingerprint, Message, Environment, Application, SeverityLevel
| order by LastTimeGenerated desc";

        var appTracesQuery = $@"
AppTraces
| extend _p = todynamic(Properties)
| extend OriginalFormat = trim(' ', tostring(_p[""OriginalFormat""]))
| extend FingerprintSource = iif(isempty(OriginalFormat), tostring(Message), OriginalFormat)
| project
    TimeGenerated,
    Fingerprint = base64_encode_tostring(tostring(hash(FingerprintSource))),
    Message = tostring(Message),
    Environment = trim(' ', tostring(_p[""AspNetCoreEnvironment""])),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = toint(SeverityLevel)
{envFilter}
| summarize
    Count = count(),
    LastTimeGenerated = max(TimeGenerated)
    by Fingerprint, Message, Environment, Application, SeverityLevel
| order by LastTimeGenerated desc";

        var appRequestsQuery = $@"
AppRequests
| extend _p = todynamic(Properties)
| project
    TimeGenerated,
    Fingerprint = base64_encode_tostring(tostring(hash(Name))),
    Message = tostring(Name),
    Environment = trim(' ', tostring(_p[""AspNetCoreEnvironment""])),
    Application = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    SeverityLevel = iif(tobool(coalesce(Success, true)), 1, 3)
{envFilter}
| summarize
    Count = count(),
    LastTimeGenerated = max(TimeGenerated)
    by Fingerprint, Message, Environment, Application, SeverityLevel
| order by LastTimeGenerated desc";

        async IAsyncEnumerable<SummarySubset> Run(string query, LogSource source)
        {
            var response = await client.QueryWorkspaceAsync(
                workspaceId,
                query,
                new QueryTimeRange(timeSpan));

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
                yield return new SummarySubset
                {
                    Fingerprint = row[fp]!.ToString()!,
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

    private LogsQueryClient GetClient(IApplicationInsightsContext context = null)
    {
        if (context.IsCurrent()) context = null;

        var clientSecret = context?.ClientSecret ?? _options.ClientSecret;
        var tenantId = context?.TenantId ?? _options.TenantId;
        var clientId = context?.ClientId ?? _options.ClientId;

        if (string.IsNullOrEmpty(clientSecret)) throw new InvalidOperationException($"No {nameof(ApplicationInsightsOptions.ClientSecret)} has been configured.");
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var client = new LogsQueryClient(clientSecretCredential);
        return client;
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

        return DateTime.Parse(value.ToString()!);
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

        return int.Parse(value.ToString()!);
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

public record EnvironmentOption
{
    public string Label { get; set; }
    public string Value { get; set; }

    public EnvironmentOption(string value, string label = null)
    {
        Value = value;
        Label = label ?? value ?? "Any";
    }

    public static implicit operator string(EnvironmentOption option) => option?.Value;
}