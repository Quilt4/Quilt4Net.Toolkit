# Quilt4Net.Toolkit.Mcp

MCP (Model Context Protocol) provider for Quilt4Net.Toolkit. Plugs into the [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) ecosystem and exposes the Application Insights query surface (`IApplicationInsightsService`) as MCP tools and resources, so AI agents (Claude Desktop, Cursor, Claude Code, etc.) can look up incidents and correlate logs without writing KQL.

## Get started

Install the NuGet package and register the provider alongside any other MCP providers (`Tharga.Platform.Mcp` for auth, `Tharga.MongoDB.Mcp`, etc.):

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddPlatform();                 // optional — auth via Tharga.Platform
    mcp.AddQuilt4Net(o =>
    {
        o.DataAccess = Quilt4Net.Toolkit.Mcp.DataAccessLevel.DataRead; // default: Metadata
    });
});

app.UseThargaMcp();                    // maps /mcp/system, /mcp/team, /mcp/me
```

The provider also requires `Quilt4Net.Toolkit`'s Application Insights services — typically registered via:

```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```

## Exposed tools (`McpScope.System`)

| Tool | Sensitivity | Args | Returns |
|---|---|---|---|
| `quilt4net.get_environments` | Metadata | — | environment names seen in the workspace |
| `quilt4net.search_logs` | DataRead | `text`, `environment?`, `lookbackHours?`, `minSeverity?` | up to 100 `LogItem`s |
| `quilt4net.get_log_detail` | DataRead | `id`, `source`, `environment?`, `lookbackHours?` | full `LogDetails` for one row |
| `quilt4net.list_summaries` | DataRead | `environment?`, `lookbackHours?` | up to 100 `SummarySubset`s |
| `quilt4net.get_summary` | DataRead | `fingerprint`, `source`, `environment?`, `lookbackHours?` | grouped `SummaryData` for one fingerprint |
| `quilt4net.lookup_incident` | DataRead | `incidentId`, `lookbackHours?` | rows whose `Properties.IncidentId` matches |
| `quilt4net.lookup_correlation` | DataRead | `correlationId`, `lookbackHours?` | rows whose `OperationId` / `Properties.CorrelationId` matches |

## Exposed resources

| URI | Description |
|---|---|
| `quilt4net://environments` | environment names seen in the workspace |
| `quilt4net://summaries` | recent summary buckets (last 24h by default) |

## Options

| Property | Default | Description |
|---|---|---|
| `DataAccess` | `Metadata` | `Metadata` exposes only `get_environments` and the resources. `DataRead` enables search/detail/summary/lookup tools. `DataReadWrite` is reserved for a future write-tools phase. |
| `DefaultLookback` | `1d` | Lookback window when a tool call doesn't specify `lookbackHours`. |
| `MaxLookback` | `7d` | Server-side cap on `lookbackHours` to bound query cost. |

## Phase

This is Phase 1 of the Quilt4Net MCP plan — Application Insights logs (read). Health / Version / Dependencies / Components and write tools land in follow-up packages.
