# Log views

Drop-in Blazor components that render Application Insights data fetched via `IApplicationInsightsService`. The same components power Quilt4Net Server's `/monitor/log` and `/developer/log` pages.

## Minimum

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Log

<LogView />
```

Renders the Search / Summary / Measure / Count tabs against the AI workspace configured by `AddQuilt4NetApplicationInsightsClient`. Detail and Summary drill-downs open in a Radzen dialog.

## Supplying configurations

`LogView` queries one workspace at a time. Point it at one, in precedence order:

| Source | Use when |
|---|---|
| `Context` | A single workspace (or the locally configured one). |
| `Configs` | You already hold an explicit set (e.g. a team's). `LogView` renders a dropdown to pick one when more than one is supplied. |
| DI selector | You registered `AddQuilt4NetBlazorApplicationInsightsClientRemote`; the built-in dropdown is driven by it. |

`Configs` is an `IReadOnlyList<ApplicationInsightsConfigurationResponse>`, so a host that already has the team's workspaces in hand can render `LogView` directly instead of wrapping it in a custom picker:

```razor
<LogView Configs="@_workspaces" FilterStorageScope="@team.Key" />
```

Precedence: `Context` > selected `Configs` entry > DI selector.

## Dedicated detail / summary pages

```razor
@page "/log"
<LogView Tab="@Tab" DetailPath="/log/detail" SummaryPath="/log/summary" />

@code {
    [Parameter, SupplyParameterFromQuery] public string Tab { get; set; }
}
```

```razor
@page "/log/detail/{Id}"
<LogDetailView Id="@Id" Params="@P" SummaryPath="/log/summary" />

@code {
    [Parameter] public string Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public string P { get; set; }
}
```

`?tab=...` is honoured (and updated) by `LogView` as the user switches tabs — deep linking and browser back/forward work out of the box.

## Application alias rendering

Real-world AI data often spreads one logical app across multiple `cloud_RoleName` values (e.g. `MyApp.Server` + `MyApp.Server.Client` for a Blazor Server + WASM combo). Pass an `ApplicationAliasMap` to render every Application cell as the logical name with the raw value on hover:

```razor
<LogView ApplicationAliasMap="@_aliasMap" />

@code {
    private readonly Dictionary<string, string> _aliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MyApp.Server"] = "myapp-web",
        ["MyApp.Server.Client"] = "myapp-web"
    };
}
```

For static maps shared across the whole app, set `ApplicationInsightsOptions.ApplicationAlias` once at startup and the toolkit picks it up — no per-page wiring needed.

## Filter bar (Source / Level / Application / Environment)

The Search and Summary tabs each render a `<LogFilterBar>` above the grid — four `RadzenSelectBar Multi="true"` rows. Source and Level options come from the enums; Application and Environment options are populated from the loaded data's distinct values (with alias resolution applied to applications). Filtering replaces Radzen's per-column filter inputs.

`FilterStorageScope` enables browser-`localStorage` persistence keyed under `Quilt4Net.Log.Filters.{scope}.{tab}`:

```razor
<LogView FilterStorageScope="@team.Key" />
```

The user's filter selection on each tab survives reloads and is isolated per scope value.

## CorrelationId column on Search

The Search tab renders a 140 px **Correlation** column showing the first 8 chars of the GUID with a hover tooltip and a copy button:

```
1a2b3c4d…  📋
```

Click the preview → Search re-runs scoped to that correlation id. `LogItem.CorrelationId` is populated automatically when your apps emit it as a structured property; `Quilt4Net.Toolkit.Api`'s `CorrelationIdMiddleware` does this via a logging scope (see [Telemetry identity & correlation](telemetry-identity.md)).

## Stack Trace tab + Resharper-friendly file:line

The Detail view's **Stack Trace** sub-tab parses `AppExceptions.Details[].parsedStack` into a grid:

| # | Assembly | Method | File | Line | Copy |
|---|---|---|---|---|---|
| 0 | MyApp.Server | `MyApp.Foo.Bar` | UserService.cs | 24 | 📋 |

The "Show frames without file/line" toggle hides system / framework frames by default. The per-row CopyButton produces a `:line N`-suffixed path. Configure `SourcePathRoots` to shorten paths so an IDE can find the file:

```razor
<LogView SourcePathRoots="@(new[] { "MyApp.Server" })" />
```

A frame whose `fileName` is `D:\a\1\s\MyApp.Server\Features\Team\UserService.cs` line 24 → clipboard text `\Features\Team\UserService.cs:line 24`. Paste-ready into Rider's "Find file" prompt.

The Stack Trace tab is exception-only; trace and request rows show a friendly "no parsed stack trace available" alert instead of an empty grid.

## Test tab (developer-only)

```razor
<LogView ShowTestTab="true" />
```

Adds a tab that triggers traces and exceptions at every `LogLevel` via `ILogger`, plus an uncaught throw caught by `Tharga.Blazor.CustomErrorBoundary` so the correlation guid surfaces in the recovery banner and in `customDimensions["CorrelationId"]`. Off by default so external apps don't accidentally expose log-injection controls.

## Standalone count components

`LogView` exposes a `Count` tab, but the two underlying components — `LogCountView` and `LogCountByServiceView` — can also be placed on their own when a host wants the volume view without the search / summary / measure tabs around it.

### `LogCountView` — per-action histogram

A bar chart (and tabular companion) of log volume grouped by Action, optionally faceted by Environment. Same component Quilt4Net Server's `/developer/log` Count tab uses.

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Log
@using Quilt4Net.Toolkit.Features.ApplicationInsights

<LogCountView Context="@ApplicationInsightsContextExtensions.Current"
              Range="TimeSpan.FromHours(1)" />
```

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Context` | `IApplicationInsightsContext` | `null` | The workspace to query. When null, resolved via the DI selector / configured options. |
| `Environment` | `string` | `null` | Restrict the query to one environment. When null, every environment in the workspace is included and each series is keyed `Environment.Action`. |
| `Range` | `TimeSpan` | `1 day` | Lookback window. |

Cascading `LogNavigationOptions` is honoured for detail drill-downs — when a host sets `DetailPath`, clicking a grid row navigates instead of opening the Radzen dialog.

### `LogCountByServiceView` — service × severity pivot

A pivot of log counts grouped by service across the Verbose / Information / Warning / Error / Critical severity columns, with multi-select filters for Configuration, Environment and Source, plus a "Per machine" toggle that splits each service row by machine. Every filter except Range is a local regroup over the cached cell cube — only Range changes hit AI. Same component Quilt4Net Server's `/developer/log-count` page uses.

A **Show: Count / Volume** toggle flips every cell, total and pie between record count and billed ingestion volume (`_BilledSize`, auto-scaled GB/MB/KB) — a pure local recompute, since both ride on every cached cell. Two marginal pies sit below the grid: **By severity** (the table's bottom totals) and **By service** (its right totals), both following the active metric and filters.

A **Sampling** section reports what the current selection would have ingested with no [ingestion sampling](https://learn.microsoft.com/azure/azure-monitor/app/sampling). Each cell carries a sampling-corrected `TrueCount = sum(ItemCount)` and `TrueBytes = sum(_BilledSize * ItemCount)` beside the retained figures; the section shows the overall effective rate ("X% of records retained, showing A of an estimated B without sampling") and a per-source table (Retained / Est. true / Retained %). When `ItemCount` is 1 everywhere — i.e. nothing is sampled — it says so plainly, which is itself the answer to "are we sampling?". `AppMetrics` is excluded (its `ItemCount` is an aggregation count, not a sampling weight).

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Log

<LogCountByServiceView Configs="@_workspaces" />
```

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Context` | `IApplicationInsightsContext` | `null` | Explicit single workspace. Bypasses the configuration multi-select. |
| `Configs` | `IReadOnlyList<ApplicationInsightsConfigurationResponse>` | `null` | Explicit set; multi-select bar lists each as a chip — *nothing selected* means aggregate across all. Takes precedence over the DI selector; ignored when `Context` is set. |
| `EnvironmentOrder` | `IReadOnlyList<string>` | `null` | Preferred display order for environment chips. Named entries come first; extras fall back to alphabetical. When null, `EnvironmentOrdering.DefaultOrder` (Development, CI, Staging, Test, Production) is used. Hosts pulling team-specific ordering from their own config (e.g. via `IEnvironmentService` on Quilt4Net.Server) should pass it here. |

Ranges (1h / 24h / 7d) and the Refresh button work the same way as in [`MetricsView`](metrics.md) — the cell cube is cached per circuit in a scoped `LogCountCellCache` for 10 minutes so range flips and page-navigation revisits skip the AI round-trip.

### Remote vs local mode

The same `Context` precedence applies to `LogView` / `VersionMatrixDisplay` / `MetricsView` / `LogCountByServiceView`. Hosts that already resolved a `Context` from local options *and* registered the remote selector can pick one explicitly — see [Metrics → Remote vs local mode](metrics.md#remote-vs-local-mode).

## Cost & volume

Two components turn the same workspace into a cost picture — what you ingest, and how close it runs to the daily cap. Both read billing-grade data from the `Usage` table (and, for the cap, the `Operation` table), so the numbers reconcile with the Azure bill rather than with sampled telemetry counts.

### `LogVolumeView` — billed ingestion by source

Billed ingestion volume grouped by source table (`DataType`): a pie + sortable table (Source / Size / Share) for the **1h**, **24h**, **Today (UTC)** and **Yesterday (UTC)** ranges, and a **per-source line chart** for the **7d** range. A total for the window is shown above. Size auto-scales to GB/MB/KB; the column sorts on the raw value so ordering is correct regardless of the unit shown.

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Log

<LogVolumeView Context="@ApplicationInsightsContextExtensions.Current"
               Range="TimeSpan.FromDays(7)" />
```

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Context` | `IApplicationInsightsContext` | `null` | Workspace to query. When null, resolved via the DI selector / configured options. |
| `Range` | `TimeSpan` | `7 days` | Initial range; the in-view **1h / 24h / Today / Yesterday / 7d** selector changes it. |

`Usage` is billing-grade but reported with a delay, so short ranges (1h/Today) can lag behind real-time — a caption notes this. When the `Usage` table can't be read (or is empty for the range) the view shows an informational alert instead of an empty chart.

### `CapTimelineView` — daily ingestion-cap graph

Tracks billed volume against the daily cap, detected from the workspace's daily-cap `Operation` events. Two views via the **Break by** toggle:

- **Calendar day** (default) — one column per UTC day. "Capped for" shows the total no-collection time that day (a gap spanning the reset splits across midnight); its tooltip lists the capped sub-intervals, e.g. `Capped 00:00–12:00, 22:24–24:00 UTC`.
- **Cap cycle** — one row per quota cycle (reset → next reset), so a cap is a single clean span (hit → next reset) with no midnight split.

Each bar stacks **Ingested** (bottom) + the **estimated capped-away** volume (top), so the full bar height is the estimated uncapped volume; a dashed reference line marks the cap. A summary shows the **measured cap size** and the **detected reset time**.

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Log

<CapTimelineView Context="@ApplicationInsightsContextExtensions.Current"
                 Days="30" />
```

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Context` | `IApplicationInsightsContext` | `null` | Workspace to query. When null, resolved via the DI selector / configured options. |
| `Days` | `int` | `30` | Initial range; the in-view **14 / 30 / 90 days** selector changes it. |

**Detection** (the daily cap reset hour varies per workspace, so this is data-driven): cap hits come from the `"stopped due to daily limit"` events and the reset/resume time from the `"daily limit reset"` events — precise to the event, unlike the ~1h-lagged `Usage` table. The **cap size** is measured as the billed volume in a complete reset→cap cycle (floored to whole GB, since collection stops just *past* the cap), falling back to the `"Daily quota changed to N"` config event if present. With no caps observed the view just charts daily volume; when the tables aren't readable it shows an informational alert.

Both components honour the same remote/local `Context` precedence as the other views — pass an explicit `Context`, or let the DI selector drive them. On Quilt4Net Server they appear as the **Volume** and **Daily cap** tabs on the **Log volume** page (`/monitor/volume`, `/developer/volume`).

## Where next

- **[Metrics](metrics.md)** — host telemetry (CPU, memory, disk, network) from the same workspace.
- **[Telemetry identity & correlation](telemetry-identity.md)** — how the data shown by these components reaches AI in the first place.
- **API reference** for the new types: `xref:Quilt4Net.Toolkit.Features.ApplicationInsights.LogItem.CorrelationId`, `xref:Quilt4Net.Toolkit.Features.ApplicationInsights.StackFrameParser`.
