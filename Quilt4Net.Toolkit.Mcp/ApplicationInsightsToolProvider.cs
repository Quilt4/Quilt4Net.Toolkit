using System.Text.Json;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Tharga.Mcp;

namespace Quilt4Net.Toolkit.Mcp;

/// <summary>
/// Exposes the Quilt4Net Application Insights query surface as MCP tools on
/// <see cref="McpScope.System"/>. All tools delegate to
/// <see cref="IApplicationInsightsService"/>.
/// </summary>
public sealed class ApplicationInsightsToolProvider : IMcpToolProvider
{
    internal const string GetEnvironmentsToolName = "quilt4net.get_environments";
    internal const string SearchLogsToolName = "quilt4net.search_logs";
    internal const string GetLogDetailToolName = "quilt4net.get_log_detail";
    internal const string ListSummariesToolName = "quilt4net.list_summaries";
    internal const string GetSummaryToolName = "quilt4net.get_summary";
    internal const string LookupIncidentToolName = "quilt4net.lookup_incident";
    internal const string LookupCorrelationToolName = "quilt4net.lookup_correlation";

    private static readonly JsonElement EmptyArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        { "type": "object", "properties": {} }
        """);

    private static readonly JsonElement SearchLogsArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "text": { "type": "string", "description": "Free-text fragment to find in Message or CorrelationId. Empty matches everything." },
            "environment": { "type": "string", "description": "Optional environment to filter by (e.g. \"Production\")." },
            "lookbackHours": { "type": "integer", "minimum": 1, "description": "Lookback window in hours. Defaults to the provider's DefaultLookback. Capped at MaxLookback." },
            "minSeverity": { "type": "integer", "minimum": 0, "maximum": 4, "description": "Minimum SeverityLevel (0=Verbose, 1=Info, 2=Warning, 3=Error, 4=Critical). Default 0." }
          }
        }
        """);

    private static readonly JsonElement GetLogDetailArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "id": { "type": "string", "description": "_ItemId from the AI row (the LogItem.Id field)." },
            "source": { "type": "string", "enum": ["Trace", "Exception", "Request"], "description": "Which AI table the row came from." },
            "environment": { "type": "string" },
            "lookbackHours": { "type": "integer", "minimum": 1 }
          },
          "required": ["id", "source"]
        }
        """);

    private static readonly JsonElement ListSummariesArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "environment": { "type": "string" },
            "lookbackHours": { "type": "integer", "minimum": 1 }
          }
        }
        """);

    private static readonly JsonElement GetSummaryArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "fingerprint": { "type": "string", "description": "Fingerprint from a SummarySubset / LogItem." },
            "source": { "type": "string", "enum": ["Trace", "Exception", "Request"] },
            "environment": { "type": "string" },
            "lookbackHours": { "type": "integer", "minimum": 1 }
          },
          "required": ["fingerprint", "source"]
        }
        """);

    private static readonly JsonElement LookupIncidentArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "incidentId": { "type": "string", "description": "6-char base32 incident id from a Quilt4Net UI alert." },
            "lookbackHours": { "type": "integer", "minimum": 1 }
          },
          "required": ["incidentId"]
        }
        """);

    private static readonly JsonElement LookupCorrelationArgSchema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "properties": {
            "correlationId": { "type": "string", "description": "OperationId / CorrelationId / RequestId GUID." },
            "lookbackHours": { "type": "integer", "minimum": 1 }
          },
          "required": ["correlationId"]
        }
        """);

    private static readonly McpToolDescriptor[] AllTools =
    [
        new McpToolDescriptor
        {
            Name = GetEnvironmentsToolName,
            Description = "List the environment names seen in the workspace's recent telemetry.",
            InputSchema = EmptyArgSchema
        },
        new McpToolDescriptor
        {
            Name = SearchLogsToolName,
            Description = "Search AppExceptions, AppTraces and AppRequests by free-text. Returns up to 100 LogItems (DataRead).",
            InputSchema = SearchLogsArgSchema
        },
        new McpToolDescriptor
        {
            Name = GetLogDetailToolName,
            Description = "Return the full LogDetails (raw JSON payload included) for a single AI row identified by id + source (DataRead).",
            InputSchema = GetLogDetailArgSchema
        },
        new McpToolDescriptor
        {
            Name = ListSummariesToolName,
            Description = "List grouped log summary buckets (fingerprint + count + last-seen). Up to 100 entries (DataRead).",
            InputSchema = ListSummariesArgSchema
        },
        new McpToolDescriptor
        {
            Name = GetSummaryToolName,
            Description = "Return the SummaryData for a fingerprint + source (DataRead).",
            InputSchema = GetSummaryArgSchema
        },
        new McpToolDescriptor
        {
            Name = LookupIncidentToolName,
            Description = "Look up Application Insights rows by Quilt4Net IncidentId (Properties.IncidentId match) (DataRead).",
            InputSchema = LookupIncidentArgSchema
        },
        new McpToolDescriptor
        {
            Name = LookupCorrelationToolName,
            Description = "Look up Application Insights rows by CorrelationId, OperationId, or RequestId (DataRead).",
            InputSchema = LookupCorrelationArgSchema
        }
    ];

    private static readonly IReadOnlyDictionary<string, DataAccessLevel> ToolLevels = new Dictionary<string, DataAccessLevel>
    {
        [GetEnvironmentsToolName] = DataAccessLevel.Metadata,
        [SearchLogsToolName] = DataAccessLevel.DataRead,
        [GetLogDetailToolName] = DataAccessLevel.DataRead,
        [ListSummariesToolName] = DataAccessLevel.DataRead,
        [GetSummaryToolName] = DataAccessLevel.DataRead,
        [LookupIncidentToolName] = DataAccessLevel.DataRead,
        [LookupCorrelationToolName] = DataAccessLevel.DataRead
    };

    private readonly IApplicationInsightsService _ais;
    private readonly Quilt4NetMcpOptions _options;

    public ApplicationInsightsToolProvider(IApplicationInsightsService ais, Quilt4NetMcpOptions options)
    {
        _ais = ais;
        _options = options;
    }

    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
    {
        IReadOnlyList<McpToolDescriptor> filtered = AllTools
            .Where(t => ToolLevels[t.Name] <= _options.DataAccess)
            .ToArray();
        return Task.FromResult(filtered);
    }

    public async Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
    {
        if (!ToolLevels.TryGetValue(toolName, out var required))
        {
            return Error($"Unknown tool: {toolName}");
        }
        if (required > _options.DataAccess)
        {
            return Error($"Tool '{toolName}' requires DataAccessLevel.{required} but server is configured for DataAccessLevel.{_options.DataAccess}.");
        }

        try
        {
            return toolName switch
            {
                GetEnvironmentsToolName => await GetEnvironmentsAsync(cancellationToken),
                SearchLogsToolName => await SearchLogsAsync(arguments, cancellationToken),
                GetLogDetailToolName => await GetLogDetailAsync(arguments),
                ListSummariesToolName => await ListSummariesAsync(arguments, cancellationToken),
                GetSummaryToolName => await GetSummaryAsync(arguments),
                LookupIncidentToolName => await LookupIncidentAsync(arguments, cancellationToken),
                LookupCorrelationToolName => await LookupCorrelationAsync(arguments, cancellationToken),
                _ => Error($"Unknown tool: {toolName}")
            };
        }
        catch (Exception e)
        {
            return Error($"{e.GetType().Name}: {e.Message}");
        }
    }

    private async Task<McpToolResult> GetEnvironmentsAsync(CancellationToken cancellationToken)
    {
        var items = await _ais.GetEnvironments(null)
            .Select(e => new { value = e.Value, label = e.Label })
            .ToArrayAsync(cancellationToken);
        return Ok(new { count = items.Length, environments = items });
    }

    private async Task<McpToolResult> SearchLogsAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var text = GetString(arguments, "text") ?? string.Empty;
        var environment = GetString(arguments, "environment");
        var lookback = GetLookback(arguments);
        var minSeverity = (SeverityLevel)(GetInt(arguments, "minSeverity") ?? 0);

        var items = await _ais.SearchAsync(null, environment, text, lookback, minSeverity)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Ok(new
        {
            lookbackHours = (int)lookback.TotalHours,
            count = items.Length,
            truncated = items.Length == 100,
            items
        });
    }

    private async Task<McpToolResult> GetLogDetailAsync(JsonElement arguments)
    {
        var id = arguments.GetProperty("id").GetString();
        if (!TryParseSource(arguments.GetProperty("source").GetString(), out var source))
            return Error("source must be Trace, Exception, or Request.");

        var environment = GetString(arguments, "environment");
        var lookback = GetLookback(arguments);

        var detail = await _ais.GetDetail(null, id, source, environment, lookback);
        if (detail is null) return Error($"No {source} item found with id '{id}' in the last {(int)lookback.TotalHours}h.");
        return Ok(detail);
    }

    private async Task<McpToolResult> ListSummariesAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var environment = GetString(arguments, "environment");
        var lookback = GetLookback(arguments);

        var items = await _ais.GetSummaries(null, environment, lookback)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Ok(new
        {
            lookbackHours = (int)lookback.TotalHours,
            count = items.Length,
            truncated = items.Length == 100,
            summaries = items
        });
    }

    private async Task<McpToolResult> GetSummaryAsync(JsonElement arguments)
    {
        var fingerprint = arguments.GetProperty("fingerprint").GetString();
        if (!TryParseSource(arguments.GetProperty("source").GetString(), out var source))
            return Error("source must be Trace, Exception, or Request.");

        var environment = GetString(arguments, "environment");
        var lookback = GetLookback(arguments);

        var summary = await _ais.GetSummary(null, fingerprint, source, environment, lookback);
        if (summary is null) return Error($"No {source} summary found for fingerprint '{fingerprint}' in the last {(int)lookback.TotalHours}h.");
        return Ok(summary);
    }

    private async Task<McpToolResult> LookupIncidentAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var incidentId = arguments.GetProperty("incidentId").GetString();
        if (string.IsNullOrWhiteSpace(incidentId)) return Error("incidentId is required.");
        var lookback = GetLookback(arguments);

        var items = await _ais.SearchByIncidentIdAsync(null, incidentId, lookback)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Ok(new
        {
            incidentId,
            lookbackHours = (int)lookback.TotalHours,
            count = items.Length,
            truncated = items.Length == 100,
            items
        });
    }

    private async Task<McpToolResult> LookupCorrelationAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var correlationId = arguments.GetProperty("correlationId").GetString();
        if (string.IsNullOrWhiteSpace(correlationId)) return Error("correlationId is required.");
        var lookback = GetLookback(arguments);

        var items = await _ais.SearchByCorrelationIdAsync(null, correlationId, lookback)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Ok(new
        {
            correlationId,
            lookbackHours = (int)lookback.TotalHours,
            count = items.Length,
            truncated = items.Length == 100,
            items
        });
    }

    private TimeSpan GetLookback(JsonElement arguments)
    {
        var hours = GetInt(arguments, "lookbackHours");
        if (!hours.HasValue || hours.Value <= 0) return _options.DefaultLookback;
        var requested = TimeSpan.FromHours(hours.Value);
        return requested > _options.MaxLookback ? _options.MaxLookback : requested;
    }

    private static string GetString(JsonElement arguments, string name)
    {
        if (arguments.ValueKind != JsonValueKind.Object) return null;
        if (!arguments.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind != JsonValueKind.String) return null;
        var value = prop.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? GetInt(JsonElement arguments, string name)
    {
        if (arguments.ValueKind != JsonValueKind.Object) return null;
        if (!arguments.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind != JsonValueKind.Number) return null;
        return prop.GetInt32();
    }

    private static bool TryParseSource(string value, out LogSource source)
    {
        return Enum.TryParse(value, ignoreCase: true, out source);
    }

    private static McpToolResult Ok(object payload) => new()
    {
        Content = [new McpContent { Type = "text", Text = JsonSerializer.Serialize(payload) }]
    };

    private static McpToolResult Error(string message) => new()
    {
        IsError = true,
        Content = [new McpContent { Type = "text", Text = message }]
    };
}
