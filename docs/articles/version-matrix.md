# Version matrix

`<VersionMatrixDisplay>` is a drop-in Blazor component that renders an application × environment grid showing the latest version observed for each combination, sourced from your Application Insights / Log Analytics workspace.

## Minimum

```razor
@using Quilt4Net.Toolkit.Blazor.Features.VersionMatrix
@using Quilt4Net.Toolkit.Features.ApplicationInsights

<VersionMatrixDisplay Context="@ApplicationInsightsContextExtensions.Current" />
```

The component fetches via `IVersionMatrixService` (in-memory cached, singleton — registered automatically by `AddQuilt4NetApplicationInsightsClient`), then renders. The Refresh button bypasses the cache.

## Supplying configurations

Three ways to tell the component which workspace(s) to query, in precedence order:

| Source | Use when |
|---|---|
| `Context` | You have one workspace (or the locally configured one via `ApplicationInsightsContextExtensions.Current`). |
| `Configs` | You already hold an explicit set of workspaces (e.g. a team's). A multi-select bar lets the user choose which to include — none selected merges them all. |
| DI selector | You registered `AddQuilt4NetBlazorApplicationInsightsClientRemote`; the component resolves the configurations and renders the picker itself. |

`Configs` is an `IReadOnlyList<ApplicationInsightsConfigurationResponse>`, each carrying its own credentials — so a host that already has the team's workspaces in hand can reuse this component instead of duplicating the grid:

```razor
<VersionMatrixDisplay Configs="@_workspaces" />
```

Precedence: `Context` > `Configs` > DI selector > locally configured options.

## Showing or hiding environment columns

Two toolbar toggles, **both off by default**:

- **Show Development** — the `Development` column.
- **Show Unknown** — the `(unknown)` column (cells with no environment tag).

When a toggle is off that column is hidden, **and** any application row left with no values in the remaining columns drops out too — so workspaces that only report dev / unknown data don't clutter the default view. Toggling is instant (no re-query).

## Environment ordering

By default environments render in the order **Development → CI → Staging → Test → Production**, then any unrecognised names alphabetically, then `(unknown)` last (cells with empty `cloud_RoleInstance` env tags).

Override globally via options:

```csharp
builder.AddQuilt4NetApplicationInsightsClient(o =>
{
    o.EnvironmentOrder = ["dev", "qa", "uat", "prod"];
});
```

Or per page (wins over options) — useful when the order comes from runtime data such as a per-tenant configuration:

```razor
<VersionMatrixDisplay Context="@ctx" EnvironmentOrder="@_envNames" />
```

## Application alias folding

Real-world telemetry often emits multiple `cloud_RoleName` values for what is conceptually one application — e.g. `MyApp.Server` + `MyApp.Server.Client` are both rows of the same web app. Folding collapses them into one logical row and surfaces version disagreements as a `conflict` badge.

### Static map (declarative)

For an app-wide alias map, configure once at startup:

```csharp
builder.AddQuilt4NetApplicationInsightsClient(o =>
{
    o.ApplicationAlias =
    [
        new ApplicationAliasMap
        {
            LogicalName = "myapp-web",
            SourceNames = ["MyApp.Server", "MyApp.Server.Client"]
        }
    ];
});
```

### Dynamic folder (per-component)

When the alias map is computed at request time (e.g. per-tenant from a database), pass an `AliasFolder` delegate. It wins over the options-bound static map for that component instance:

```razor
<VersionMatrixDisplay Context="@ctx" AliasFolder="@FoldAsync" />

@code {
    private async Task<VersionMatrixView> FoldAsync(VersionMatrixView raw, CancellationToken ct)
    {
        var map = await _aliasService.GetForCurrentTenantAsync(ct);
        return StaticAliasFolder.Fold(raw, map);
    }
}
```

`StaticAliasFolder` is exported so dynamic callers can reuse the exact same fold algorithm — only the source of the map differs.

## Precedence

Two pluggable knobs, same precedence pattern:

| Knob | Wins | Then | Falls back to |
|---|---|---|---|
| Alias folding | `AliasFolder` parameter | `options.ApplicationAlias` | raw view (no folding) |
| Environment order | `EnvironmentOrder` parameter | `options.EnvironmentOrder` | `["Development", "CI", "Staging", "Test", "Production"]` |

External / static consumers configure once and forget. Hosts with dynamic per-team or per-tenant rules opt into the parameters where they need them.
