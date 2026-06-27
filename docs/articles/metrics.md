# Metrics

Three Blazor components in `Quilt4Net.Toolkit.Blazor.Features.Metrics` plot machine-level telemetry (CPU, memory, disk, network) sourced from the AI workspace via `IApplicationInsightsService`. The composite `MetricsView` is the drop-in surface that powers Quilt4Net Server's **Infrastructure** page (`/monitor/infrastructure`, `/developer/infrastructure`); `MetricChart` and `MetricLatestChart` are the two primitives it renders and are exported so custom dashboards can compose their own charts off the same `MetricSample[]` stream.

## Minimum

```razor
@using Quilt4Net.Toolkit.Blazor.Features.Metrics

<MetricsView />
```

Renders CPU %, Memory %, Disk used (GB) and Network (MB/s) for the AI workspace configured by `AddQuilt4NetApplicationInsightsClient`. The Refresh button bypasses the per-circuit cache.

## Supplying configurations

`MetricsView` queries one workspace at a time. Point it at one, in precedence order:

| Source | Use when |
|---|---|
| `Context` | A single workspace (or the locally configured one). |
| `Configs` | You already hold an explicit set (e.g. a team's). The component renders a dropdown when more than one is supplied. |
| DI selector | You registered `AddQuilt4NetBlazorApplicationInsightsClientRemote`; the built-in dropdown is driven by it. |

```razor
<MetricsView Configs="@_workspaces" />
```

Precedence: `Context` > selected `Configs` entry > DI selector.

### Remote vs local mode

The same `Context` precedence applies to `LogView`, `VersionMatrixDisplay`, `MetricsView` and `LogCountByServiceView`. Hosts that already resolved a `Context` from local options *and* registered the remote selector can pick one explicitly:

```razor
@inject IServiceProvider Services

<MetricsView Context="@_context" />

@code {
    private IApplicationInsightsContext _context;

    protected override void OnInitialized()
    {
        _context = Services.GetService<IApplicationInsightsConfigurationSelector>() != null
            ? null                                          // remote mode → toolkit's in-component selector
            : ApplicationInsightsContextExtensions.Current; // local mode → configured options
    }
}
```

Passing `null` for `Context` lets the component resolve the workspace itself via the registered selector — the recommended shape for hosts that want the user-visible workspace picker in the chart's filter row.

## Range and refresh

The filter row offers fixed range buttons — **1h**, **24h**, **7d** — and a Refresh button. Each `(Context, Range)` pair is cached for 10 minutes in a per-circuit `MetricsSeriesCache`, so flipping back to a previously-loaded range is instant and survives page navigation within the same Blazor circuit. Refresh evicts the current `(Context, Range)` entry and re-fetches.

## The four charts

| Chart | Renderer | Behaviour |
|---|---|---|
| **CPU %** | `MetricChart` | Line chart per host. `FullPercentScale="true"` pins the y-axis to 0–100, preventing an idle "always between 8 and 12 %" host from looking saturated. |
| **Memory %** | `MetricChart` | Line chart per host, same 0–100 pin. |
| **Disk used (GB)** | `MetricLatestChart` | One bar per (host, volume). Bar fill represents the latest value as a fraction of capacity; turns amber > 80%, red > 90%. A delta badge (▲ +1.2 GB / 24h) signals growth. `LessIsBad="false"` because more-used is worse. |
| **Network (MB/s)** | `MetricChart` | Line chart per host with `LogScale="true"`. On a linear axis, idle hosts (~0.01 MB/s) vanish into a flat line at the bottom when one host spikes to 50 MB/s; log scale gives each host's shape its own band. |

## Composing your own dashboard

Both primitives accept a `MetricSample[]` you fetch yourself, so a host that already has `IApplicationInsightsService` injected can drop one chart anywhere:

```razor
@inject IApplicationInsightsService Ai

<MetricChart Title="CPU %" Unit="%" Samples="@_cpu" Loading="@_busy" />

@code {
    private MetricSample[] _cpu = [];
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        _busy = true;
        _cpu = await Ai.GetCpuUtilizationAsync(ApplicationInsightsContextExtensions.Current, TimeSpan.FromHours(1)).ToArrayAsync();
        _busy = false;
    }
}
```

## `MetricChart` parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Title` | `string` | (required) | Card header. |
| `Unit` | `string` | `null` | Suffix on y-axis tick labels (`"%"`, `"MB/s"`, …). |
| `Samples` | `MetricSample[]` | `[]` | Time-series points; grouped into one line per `Series`. |
| `Loading` | `bool` | `false` | When true, shows a spinner in the card header and an empty body. |
| `LogScale` | `bool` | `false` | Plot on a log10 y-axis. Reverses tick formatting back to the natural unit. Overrides `FullPercentScale` (log10(0) is −∞). |
| `FullPercentScale` | `bool` | `true` | Pin the y-axis to 0–100. Ignored when `LogScale` is on. |
| `LogScaleEpsilon` | `double` | `0.001` | Floor applied before log10 so zero / sub-noise samples don't crash the transform. |

## `MetricLatestChart` parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Title` | `string` | (required) | Card header. |
| `Unit` | `string` | `null` | Suffix on bar labels and the delta badge. |
| `Samples` | `MetricSample[]` | `[]` | Latest value per `Series` is bar-rendered; the full series powers an inline sparkline. |
| `Range` | `TimeSpan` | `default` | Window the samples cover — formats the delta-badge suffix ("/ 24h"). |
| `Loading` | `bool` | `false` | Header spinner; bars hidden while true. |
| `Capacity` | `DiskCapacity[]` | `[]` | Optional per-series total. When matched, the bar fill is `latest / capacity` instead of `latest / max-across-hosts`. |
| `LessIsBad` | `bool` | `true` | Direction of "bad". `true` (default) → less is worse (free / available); `false` → more is worse (utilisation). Drives the bar's threshold colours and the delta badge's arrow. |

## `MetricsView` parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Context` | `IApplicationInsightsContext` | `null` | Explicit single workspace. Bypasses the selector / `Configs` dropdown. |
| `Configs` | `IReadOnlyList<ApplicationInsightsConfigurationResponse>` | `null` | Explicit set; rendered as a dropdown when count > 1. Takes precedence over the DI selector; ignored when `Context` is set. |
| `StorageScope` | `string` | `null` | When set (and a `Configs` dropdown is shown), the selected configuration is remembered across sessions in browser `localStorage` under `Quilt4Net.Metrics.{scope}.config`. Hosts pass e.g. the team key. |

## Where next

- **[Log views](log-views.md)** — the sibling AI-data surface (search, summary, exceptions, counts).
- **[Version matrix](version-matrix.md)** — application × environment grid sourced from the same workspace.
- **[Telemetry identity & correlation](telemetry-identity.md)** — how the data these charts query reaches AI in the first place.
