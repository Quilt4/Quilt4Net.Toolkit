# Quilt4Net.Toolkit
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Core library providing health checks, metrics, remote configuration, feature toggles, content management, and Application Insights integration for .NET applications.

Features can be configured and monitored using [Quilt4Net Web](https://quilt4net.com).

## Feature toggles and remote configuration

Manage configuration and feature flags remotely via [Quilt4Net Web](https://quilt4net.com) or with a Blazor component from [Quilt4Net.Toolkit.Blazor](https://github.com/Quilt4/Quilt4Net.Toolkit/tree/master/Quilt4Net.Toolkit.Blazor).

Feature toggles are boolean values of remote configuration.

### Get started

Install the NuGet package [Quilt4Net.Toolkit](https://www.nuget.org/packages/Quilt4Net.Toolkit) and register the service.

```csharp
builder.AddQuilt4NetRemoteConfiguration();
```

Add your API key from [Quilt4Net Web](https://quilt4net.com) in `appsettings.json`.

```json
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

### Feature toggles

Inject `IFeatureToggleService` to check boolean feature flags.

```csharp
public class MyService
{
    private readonly IFeatureToggleService _featureToggle;

    public MyService(IFeatureToggleService featureToggle)
    {
        _featureToggle = featureToggle;
    }

    public async Task DoWorkAsync()
    {
        if (await _featureToggle.GetToggleAsync("new-feature", fallback: false))
        {
            // new feature logic
        }
    }
}
```

### Remote configuration

Inject `IRemoteConfigurationService` for typed configuration values.

```csharp
var maxRetries = await _configService.GetAsync("MaxRetries", fallback: 3);
```

### Application scoping (shared by default)

Toggles and configuration are **shared across applications by default** — a call with just a key reads
the team's shared value, so one flag controls every application that asks for it:

```csharp
// Shared (default): every application sees the same value.
await _featureToggle.GetToggleAsync("new-feature");
```

To read a value specific to **one application**, pass its name explicitly:

```csharp
await _featureToggle.GetToggleAsync("new-feature", application: "billing-service");
```

Passing `application: null` resolves to the configured `RemoteConfigurationOptions.Application`, falling
back to the entry assembly name — i.e. "this application".

> **Changed in 0.8.0:** the default scope is now **shared** (`application: ""`). Previously a bare call
> resolved to the current application. Pass `application: null` (or set `RemoteConfigurationOptions.Application`)
> for the previous per-application behavior.

### RemoteConfigurationOptions

| Property | Default | Description |
|----------|---------|-------------|
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `Application` | `null` | Application used when a read passes `application: null`. When `null`, the entry assembly name is used; set to `""` to read shared values. The read methods' own `application` parameter defaults to `""` (shared), so this option only takes effect when a call explicitly passes `null`. |
| `Ttl` | `null` | Client-requested time-to-live for cached values. When `null`, the team/server-configured default applies. |
| `HttpTimeout` | `5s` | Timeout for HTTP calls to the server. |
| `StaleWhileRevalidate` | `true` | When `true`, an expired value is returned immediately and refreshed in the background. Set `false` to refresh synchronously so callers always get a fresh value (subject to `HttpTimeout`). |
| `FailureCacheDuration` | `5s` | How long to stop calling for a key after a **failed** call. Doubles per consecutive failure up to `MaxFailureCacheDuration`, and resets on the first success. |
| `MaxFailureCacheDuration` | `5m` | Ceiling for that back-off, so a sustained outage settles into a low request rate instead of retrying every few seconds per key. |
| `MetricsEnabled` | `true` | Publish configuration resolutions on the `Quilt4Net.Toolkit.Configuration` meter (see *Metrics* below). |

Configuration path: `Quilt4Net:RemoteConfiguration`

### Metrics

Both clients publish resolution metrics, so a host gets **call volume, latency and cache-hit ratio**
without turning on `Debug` logging. Subscribe by meter name — nothing else is needed:

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(m => m
    .AddMeter(Quilt4NetMetrics.ContentMeterName)
    .AddMeter(Quilt4NetMetrics.ConfigurationMeterName));
```

| Instrument | Kind | Tags |
|---|---|---|
| `quilt4net.content.resolutions` | counter | `source`, `application`, `language`, `stale` |
| `quilt4net.content.resolution.duration` | histogram (ms) | same |
| `quilt4net.content.backoff.keys` | gauge | — |
| `quilt4net.configuration.resolutions` | counter | `source`, **`key`**, `application`, `environment`, `stale` |
| `quilt4net.configuration.resolution.duration` | histogram (ms) | same |
| `quilt4net.configuration.backoff.keys` | gauge | — |

**The cache-hit ratio is the number these exist for**, and it cannot be computed outside the library:
the public reads return a bare value, so a wrapper can time a call and never learn whether it came
from cache, stale cache, the server or a fallback. Group the counter by `source` and the ratio falls
out. A key stuck on `Fallback` is the shape of a client pinned to its default by a fault.

**The content key is deliberately not a tag** — it is unbounded (over 1,200 in one reported
application) and would blow up cardinality; per-key volume stays in the `Debug` log, which already
carries it. The **configuration key is** a tag, because a host has a handful of toggles and "which
toggle is falling back" is the question actually asked.

The two gauges report how many keys are currently held off by the failure back-off, so a client
backing off is visible directly rather than inferred from gaps between attempts.

Cost is near zero when nobody subscribes; set `MetricsEnabled` to `false` on either options type to
opt out entirely.

#### Knowing whether a value is real

`GetToggleAsync("X", false)` returning `false` cannot be told apart from a server that says `false`,
so an application pinned to its fallback by a network fault looks exactly like one deliberately
switched off. `GetToggleResultAsync` returns the value together with its provenance:

```csharp
var result = await featureToggleService.GetToggleResultAsync("AssistantPanel.Enabled", fallback: false);

if (result.Source == ConfigurationSource.Fallback)
    logger.LogWarning("Assistant panel state is the local fallback, not the server's answer.");
```

| `ConfigurationSource` | Meaning |
|---|---|
| `Server` | Fetched from Quilt4Net.Server on this call. |
| `Cache` | Served from the local cache, within its lifetime. |
| `StaleCache` | Served past its lifetime while a background refresh runs. |
| `Fallback` | The caller's fallback — nothing was reached, or the server has no value for the key. |
| `Unknown` | The implementation does not report provenance (the default interface implementation). |

A failed call is held only for `FailureCacheDuration` (widening per consecutive failure), so a
recovered server is picked up in seconds rather than at the end of a full cache lifetime.

## Content management

Manage multilingual content from [Quilt4Net Web](https://quilt4net.com).

```csharp
builder.AddQuilt4NetContent();
```

Inject `IContentService` to retrieve and manage content.

```csharp
var (value, success) = await _contentService.GetContentAsync("welcome-message", "Hello!", languageKey, ContentFormat.String);
```

#### Knowing where a value came from

`GetContentResultAsync` returns the same value plus its provenance, so you can tell a real server
value apart from a cache hit or a hard-coded fallback.

```csharp
var result = await _contentService.GetContentResultAsync("welcome-message", "Hello!", languageKey, ContentFormat.String);

if (result.Source == ContentSource.Default)
{
    // The server has no value for this key — the caller's default is being rendered.
}
```

| `ContentSource` | Meaning |
|---|---|
| `Server` | Fetched from Quilt4Net.Server on this call. |
| `Cache` | Served from the local cache, within TTL, from a value the server supplied. |
| `StaleCache` | Served from the local cache past its TTL; a background refresh was started. |
| `Default` | The caller's default was used — no override on the server (404), unreachable, or timed out. Also reported for a cached *default*, so a negative-cache hit is never mistaken for a real cache hit. |
| `Developer` | Developer language is active; every key resolves to a placeholder. |
| `NoApiKey` | No API key configured, so no lookup was attempted. |
| `Unknown` | The `IContentService` implementation does not report provenance (custom or test implementations that only override `GetContentAsync`). |

> `Success` on the legacy tuple is **not** a source discriminator — it is `true` for a cache hit, a
> stale hit and a fresh fetch alike, and `false` for a missing API key, a 404, a timeout and an
> error alike. Use `Source` when the distinction matters.

### ContentOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Application` | Assembly name | Application name. |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |
| `StaleWhileRevalidate` | `true` | When `true`, an expired value is returned immediately and refreshed in the background. Set `false` to refresh synchronously so callers always get a fresh value (subject to `HttpTimeout`). |
| `SlowLogThreshold` | `3s` | When a content fetch or language-list load from the server takes at least this long, a single `Warning` is logged (endpoint, elapsed, HTTP status) — so slow loads surface even with `Debug` off. Set `TimeSpan.Zero` to disable. |
| `WarmUpEnabled` | `true` | Pre-fill the cache at startup with one bulk call per language (Blazor). Set `false` for lazy per-key loading only. |
| `WarmUpLanguages` | `[]` | Extra languages to warm at startup and on "Reload Content", by **name** (e.g. `["English", "Svenska"]`), on top of the always-warmed default. Empty = only the default warms at startup; others warm per-circuit on first selection. |
| `PeriodicWarmUpEnabled` | `true` | Repeat the bulk warm-up on a timer instead of once per process, so entries are replaced shortly **before** they expire and the per-key path is never reached in steady state. |
| `WarmUpRefreshFraction` | `0.8` | Where in the server's observed content lifetime the re-warm runs. `0.8` of a 10-minute lifetime re-warms every 8 minutes. |
| `MinimumWarmUpInterval` | `30s` | Floor for that interval, so a very short server lifetime cannot turn the re-warm into its own source of load. |
| `FailureCacheDuration` | `5s` | How long to stop calling for a key after a **failed** call (timeout, transport error, non-404 error status). Doubles per consecutive failure up to `MaxFailureCacheDuration`, and resets on the first success. |
| `MaxFailureCacheDuration` | `5m` | Ceiling for that back-off. |
| `CacheDuration` | `null` | Cache lifetime to **request** from the server. `null` uses the server's own. The server clamps a value above its maximum rather than refusing it. |
| `MetricsEnabled` | `true` | Publish content resolutions on the `Quilt4Net.Toolkit.Content` meter (see *Metrics* below). |
| `NotFoundCacheDuration` | `10m` | How long to remember a `404` — the server was reached and has no override for the key. Deliberately far longer than the failure back-off: a 404 is an answer, and re-asking every few seconds would re-request every unseeded key on nearly every render. |

Configuration path: `Quilt4Net:Content`

#### Request volume under load

Three behaviours decide how much traffic a content-heavy application generates, and they are meant
to be read together:

- **The bulk warm-up replaces the per-key fan-out.** Every warmed key shares one expiry, so without
  a repeat the whole set goes stale at the same instant and the next render issues one HTTP call per
  key. `PeriodicWarmUpEnabled` re-warms before that happens, and covers every language actually in
  use — configured ones plus any a user selected at runtime.
- **A failed call is held briefly, not for a cache lifetime.** Before, a failure was re-stamped with
  a full TTL, so every expiry landed on one failed call that bought another whole lifetime of the
  fallback value; there was no state in which a key converged while calls kept failing.
- **A `429` is honoured exactly, on every path.** `Retry-After` overrides the local back-off in both
  directions — the server saying when it will be ready beats any client-side guess. This now covers
  content, **remote configuration** and the **bulk warm-up**; a shed call is logged at `Warning` as
  backpressure rather than at `Error` as a fault.
- **A rate-limited warm-up waits rather than falling back.** Dropping to per-key fetching would turn
  one shed call into hundreds — the exact burst the server shed it to avoid — so a `429` carrying a
  short `Retry-After` (up to 30 s) is waited out and retried once. A longer one is left to the next
  periodic re-warm rather than parked on a background task.
- **`CacheDuration` sets how often the re-warm runs at all.** The re-warm fires at
  `WarmUpRefreshFraction` of the lifetime the **server** reports, so asking for a longer one retunes it
  automatically: 24 hours at the default 0.8 is one bulk call per language every 19.2 hours instead of
  every 8 minutes. Nothing else needs changing. The trade-off is that a content edit takes up to that
  long to reach a running instance — "Reload Content" in the admin UI remains the immediate path.

#### Diagnostic logging

Content logging is split by whether a condition is **actionable**. These fire without any log
configuration:

| Condition | Level | Frequency |
|---|---|---|
| A key has no value on the server (404) | `Information` | Once per key per `FailureCacheDuration` — the negative cache stops it repeating per render. |
| No `ApiKey` configured, so content can never load | `Warning` | Once per process. |
| Server round-trip slower than `SlowLogThreshold` | `Warning` | Per slow call. |
| Timeout, or a non-404 failure response | `Warning` / `Error` | Per occurrence. |

A normal server fetch stays at `Debug` — it is the designed happy path, and at cold start it is one
line per key.

To diagnose slow or looping content/language loads, enable `Debug` logging for the content categories — no code change required:

```json
{
  "Logging": {
    "LogLevel": {
      "Quilt4Net.Toolkit.Features.Content.RemoteContentCallService": "Debug",
      "Quilt4Net.Toolkit.Blazor.LanguageStateService": "Debug"
    }
  }
}
```

At `Debug` you get, per content read, the key, resolved language + application, the resolved
`ContentSource` and elapsed time; per language load, the source (cache vs server), count and elapsed
time; and, in Blazor, each language reload (with timing) and every selected-language change that
triggers a content reload. Genuinely slow server round-trips are logged at `Warning` regardless (see `SlowLogThreshold`).

## Health check client

Client for consuming health endpoints from a remote service that uses Quilt4Net Health API.

```csharp
builder.AddQuilt4NetHealthClient(o =>
{
    o.HealthAddress = "https://my-service.example.com/api/Health/";
});
```

Inject `IHealthClient` to call remote health endpoints.

```csharp
var health = await _healthClient.GetHealthAsync(cancellationToken);
var metrics = await _healthClient.GetMetricsAsync(cancellationToken);
var version = await _healthClient.GetVersionAsync(cancellationToken);
```

If the remote endpoint replies with a non-success status code, these methods throw `HttpRequestException` (carrying the status code) rather than a JSON parse error — wrap calls in try/catch when the remote service may be unavailable.

### HealthClientOptions

| Property | Default | Description |
|----------|---------|-------------|
| `HealthAddress` | `null` | Address to the remote health API. |

Configuration path: `Quilt4Net:HealthClient`

## Application Insights client

Client for querying Application Insights data (logs, metrics, exceptions). The toolkit supports two mutually exclusive registration modes:

**Local mode** — credentials in the consumer's `appsettings.json`:
```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```

**Remote mode** — credentials fetched from Quilt4Net.Server at runtime using an API key with the `monitor:read` scope. Consumers no longer need to keep `TenantId` / `WorkspaceId` / `ClientId` / `ClientSecret` in their own configuration:
```csharp
builder.AddQuilt4NetApplicationInsightsClientRemote();
```
```json
{
  "Quilt4Net": {
    "RemoteConfiguration": {
      "Quilt4NetAddress": "https://quilt4net.com/",
      "ApiKey": "<monitor:read API key>"
    }
  }
}
```
Configuration path: `Quilt4Net:RemoteConfiguration` (the API key is also accepted at the top-level `Quilt4Net:ApiKey`). Keep the key in user-secrets or environment variables, not in committed config.

The remote provider caches the configuration list per its configured TTL (`RemoteConfigurationOptions.Ttl`, or the server default when unset) with stale-while-revalidate, so transient server outages don't break the consuming page. When more than one workspace is configured on the server for the team, every workspace is reachable; the Blazor `LogView` renders an in-component **dropdown** (one workspace) and `VersionMatrixDisplay` a **multi-select radio bar** that merges the matrix across the selected workspaces — selecting none shows all (see [Quilt4Net.Toolkit.Blazor README](https://github.com/Quilt4/Quilt4Net.Toolkit/blob/master/Quilt4Net.Toolkit.Blazor/README.md)).

> **Local and remote are mutually exclusive.** Register one or the other. If both a `Quilt4Net:ApplicationInsights` block and a remote `Quilt4Net:RemoteConfiguration` API key are configured, the remote source wins and the local block is silently ignored. In a Blazor host, use `AddQuilt4NetBlazorApplicationInsightsClientRemote()` so the workspace selector is wired up.

### ApplicationInsightsOptions

| Property | Default | Description |
|----------|---------|-------------|
| `TenantId` | `null` | Azure AD tenant ID (found under "Tenant properties" in Azure portal). Only required when `AuthMode = ClientSecret`. |
| `WorkspaceId` | `null` | Application Insights workspace ID. |
| `ClientId` | `null` | For `ClientSecret`: app registration client ID with `Data.Read` permission on Application Insights API. For `ManagedIdentity`: empty for system-assigned MI, or the user-assigned MI's client ID. For `DefaultAzureCredential`: optional hint, used as the preferred user-assigned MI when MI lights up in the chain. |
| `ClientSecret` | `null` | Client secret for the app registration. Only required when `AuthMode = ClientSecret`. |
| `AuthMode` | `ClientSecret` | Authentication mode: `ClientSecret` (service principal), `ManagedIdentity` (Azure-hosted apps), or `DefaultAzureCredential` (chained — same config works locally via `az login` and in Azure via MI). |
| `EnvironmentOrder` | `["Development", "CI", "Staging", "Test", "Production"]` | Preferred environment ordering for the version matrix. Names not listed render after, alphabetically; rows with empty env render last as `(unknown)`. |
| `ApplicationAlias` | `[]` | Static `raw → logical` alias map for `VersionMatrixDisplay` consumers that don't pass a per-component `AliasFolder` delegate. Each entry groups one or more raw `cloud_RoleName` values under a single logical application name. |

Configuration path: `Quilt4Net:ApplicationInsights`

#### Managed Identity

When the app runs in Azure (App Service, Container Apps, VMs, …) you can skip the client secret entirely and authenticate with the hosting identity:

```json
{
  "Quilt4Net": {
    "ApplicationInsights": {
      "WorkspaceId": "your-workspace-id",
      "AuthMode": "ManagedIdentity"
    }
  }
}
```

Grant the App Service identity the **Log Analytics Reader** (or Monitoring Reader) role on the target workspace. Use a user-assigned MI by setting `ClientId` to the identity's client ID; leave it empty for system-assigned.

#### DefaultAzureCredential

Use `DefaultAzureCredential` to share a single configuration across local dev and Azure-hosted environments:

```json
{
  "Quilt4Net": {
    "ApplicationInsights": {
      "WorkspaceId": "your-workspace-id",
      "AuthMode": "DefaultAzureCredential"
    }
  }
}
```

The chained credential probes (in order): environment variables → workload identity → Managed Identity → Visual Studio / VS Code account → Azure CLI (`az login`) → Azure PowerShell. The first that succeeds is used.

Typical setup:

- **Local development**: developer runs `az login` once. The toolkit picks up that token and queries the workspace directly — no service principal secret to copy into user-secrets.
- **Azure**: the App Service identity is used (same effect as `ManagedIdentity`). Grant it Log Analytics Reader as above.

`TenantId` and `ClientId` are forwarded as hints (filter to a specific tenant; prefer a specific user-assigned MI) — both can be left empty.

> **Trade-off**: `DefaultAzureCredential` masks *which* underlying credential succeeded. If authentication fails, the error chain is less specific than the explicit modes. For diagnosis, switch to `ClientSecret` or `ManagedIdentity` to isolate the issue.

## Value Groups

A **Value Group** is a server-curated bundle of references to existing values across multiple stores (today: feature toggles and Application Insights configurations; KV pairs and Atlas credentials come in later features). An external agent uses one HTTP call with its own group-scoped API key to receive a typed bundle containing only the values the operator allowlisted.

Use this when an agent needs least-privilege access to a specific deployment's configuration without exposing the team-wide scope.

```csharp
builder.AddQuilt4NetValueGroupClient(o =>
{
    o.ApiKey = builder.Configuration["Quilt4Net:ValueGroup:ApiKey"];
});
```

Then inject and call:

```csharp
public class MyAgent(IValueGroupClient client)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var bundle = await client.GetAsync(ct);
        foreach (var toggle in bundle.FeatureToggles) { /* ... */ }
        foreach (var ai in bundle.ApplicationInsightsConfigurations) { /* ... */ }
    }
}
```

### ValueGroupClientOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Quilt4NetAddress` | `https://quilt4net.com/` | Server base URL. |
| `ApiKey` | — *(required)* | The API key minted for this Value Group in the server's admin UI. Must carry the `valuegroup:read` scope. The key is tag-bound server-side to exactly one Value Group; the server resolves which group from the key, so the client never names a group id. |
| `Ttl` | `5 min` | Cache freshness window. Subsequent calls within the window serve the cached bundle. |
| `HttpTimeout` | `5 s` | HTTP timeout. On timeout the cached bundle is served if available. |

Configuration path: `Quilt4Net:ValueGroup` (or top-level `Quilt4Net:ApiKey` + `Quilt4Net:Quilt4NetAddress` for shared keys).

### Behaviour and contract

- **Stale-while-revalidate**: returning a fresh bundle is the default. Stale-cache fallback applies on transient HTTP errors *and* on timeout.
- **`ValueGroupAuthorizationException` on 401/403**: a revoked key or wrong-binding response *throws*, by design. Unlike `IRemoteConfigurationService` (which silently serves fallback values), Value Groups carry secret-bearing data, so the consumer must learn it has been revoked rather than continue using cached secrets.
- **One client = one group**: register multiple clients via keyed services if the consumer needs more than one group.

### Minting a key

In the Quilt4Net.Server admin UI under **Value Groups**: select the group → API Keys panel → **Mint new key**. The raw key is shown exactly once — save it immediately. The key carries only the `valuegroup:read` scope and is tag-bound on the server side to this one group; it cannot reach any other team data.

## Issue tracker

A per-team issue tracker, meant to be driven by an agent over REST or MCP rather than by hand. Each issue carries a title, body text, a **route** (the roadmap lane it belongs to), a Now/Next/Later band, a workflow state, an optional assignee, an optional S/M/L effort and an optional Critical/Important/Nice importance. Issues are linked to each other with typed dependencies.

```csharp
builder.AddQuilt4NetIssues(o =>
{
    o.ApiKey = builder.Configuration["Quilt4Net:Issue:ApiKey"];
});
```

Then inject and call:

```csharp
public class MyAgent(IIssueService issues)
{
    public async Task TriageAsync(CancellationToken ct)
    {
        var created = await issues.CreateAsync(new CreateIssueRequest
        {
            Title = "Bulk warm-up fans out on a shed call",
            Route = "read path",
            Band = RoadmapBand.Now,
            Effort = IssueEffort.M
        }, ct);

        await issues.AddLinkAsync(created.Number, new AddIssueLinkRequest
        {
            TargetNumber = 12,
            Kind = IssueLinkKind.Blocks,
            Reason = "the limiter has to ship before the client can honour it"
        }, ct);
    }
}
```

### Importance and effort

`Importance` (`Critical` / `Important` / `Nice`) and `Effort` (`S` / `M` / `L`) are the backlog's own vocabulary, so a tracker issue and a backlog row can be compared without translating between them. Together they give the house ordering rule: **importance first, then effort ascending** — highest impact for least work.

Both are **optional**. An issue with no importance means nobody has graded it yet, which is worth seeing; defaulting it to `Nice` would assert a judgement no one made and hide what still needs triage. Ungraded issues sort after everything graded.

Importance is deliberately **not** drawn on the roadmap. A map restates effort only — quick wins are invisible without it — while status, priority and ownership are read from the source rather than copied onto the figure.

### The three link kinds

| Kind | Drawn | Means |
|------|-------|-------|
| `Blocks` | solid | The target genuinely cannot start until the source ships. A hard constraint, and rare. |
| `Cheapens` | dashed | The target is cheaper, safer or better-informed afterwards, but is not prevented from starting. Most real edges are these. |
| `Overlaps` | dotted | Both touch the same surface, so doing them independently causes rework. Not an ordering — a warning to pick one owner. |

**Every link requires a `Reason`, and the server rejects an empty one.** Most items people assume are ordered turn out to be merely related; making the reason mandatory is what keeps the graph from filling up with dependencies nobody can justify. `Blocks` links may not form a cycle.

### Workflow

Each team has one workflow — a list of states plus the transitions allowed between them — seeded as `Todo → Doing → Done`. Read it with `GetWorkflowAsync`, replace it with `SetWorkflowAsync`. A state change goes through `SetStateAsync` rather than `UpdateAsync` so the move can be checked; a transition the workflow does not permit is rejected, and the error names the states the issue can actually reach.

A replacement workflow that would leave issues stranded in a state it no longer defines is rejected rather than applied.

### IssueOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Quilt4NetAddress` | `https://quilt4net.com/` | Server base URL. |
| `ApiKey` | — *(required)* | Team API key. Reading needs `issue:read`; writing needs `issue:write`. |

Configuration path: `Quilt4Net:Issue` (or top-level `Quilt4Net:ApiKey` + `Quilt4Net:Quilt4NetAddress` for shared keys).

### Behaviour and contract

- **Calls throw rather than degrade.** Unlike `IRemoteConfigurationService`, which serves a fallback when the server is unreachable, a tracker read that quietly returned an empty set would be indistinguishable from a team with no issues. Failures surface as `IssueServiceException` carrying the status code.
- **A 429 carries the server's `Retry-After`** on `IssueServiceException.RetryAfter`. Honour it; do not retry immediately.
- **`UpdateAsync` replaces, it does not merge.** Read the issue, change what you mean to change, and send the whole thing back. An omitted optional field is cleared.
- **Enums travel as names**, not numbers, so inserting a member never re-grades values already in flight.

### Minting a key

In the Quilt4Net.Server admin UI under **Api → Api Key**. `issue:read` comes with any key at Viewer level or above. **`issue:write` is not granted by access level** and must be added to the key explicitly as a scope override — otherwise every application key that already holds `content:write` would be able to edit the tracker. For MCP access the key also needs `mcp:discover`.

## Universal telemetry identity

`AddQuilt4NetLogging()` configures OpenTelemetry resource attributes **and** registers two `BaseProcessor`s — one for `LogRecord`, one for `Activity` — that copy a fixed set of identity attributes onto every per-record Properties bag. Works for all app types; the Azure Monitor exporter forwards the per-record attributes into `customDimensions`, where KQL can read them.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQuilt4NetLogging();
```

Up to six attributes attached to every `AppTrace`, `AppException`, `AppRequest` (and outbound `AppDependency`):

| Attribute key | Default value | Notes |
|---|---|---|
| `service.name` | `IHostEnvironment.ApplicationName` → entry assembly name (framework assemblies excluded) | Also surfaces as the `cloud_RoleName` column. |
| `service.version` | Entry assembly version | Also surfaces as `application_Version`. |
| `host.name` | `Environment.MachineName` | Also surfaces as `cloud_RoleInstance` *unless* `ServiceInstanceId` is set (see below) — in which case the variant id wins for `cloud_RoleInstance` and `host.name` keeps the machine name in `customDimensions`. |
| `deployment.environment` | `IHostEnvironment.EnvironmentName` → `DOTNET_ENVIRONMENT` → `ASPNETCORE_ENVIRONMENT` → `"Production"` | The Azure Monitor exporter does **not** forward arbitrary OTel resource attributes into per-row `Properties`, so the per-record processor copies it in too. |
| `quilt4net.monitor` | `"Quilt4Net"` (configurable via `MonitorName`) | Identifies which instrumentation produced the row. Useful when several Quilt4Net-hosted services ship to the same workspace. |
| `service.instance.id` | *unset by default* — see "Distinguishing multiple deployments of the same binary" below. | Only emitted per-record when explicitly configured. Existing consumers see no new attribute. |

Override via callback or `appsettings.json`:

```csharp
builder.AddQuilt4NetLogging(o =>
{
    o.ApplicationName = "florida-server";
    o.Version = "2.0.0";
    o.Environment = "Production";
    o.MonitorName = "florida";
});
```

```json
{
  "Quilt4Net": {
    "Logging": {
      "ApplicationName": "florida-server",
      "Version": "2.0.0",
      "Environment": "Production",
      "MonitorName": "florida"
    }
  }
}
```

`AddQuilt4NetLogging()` returns a `Quilt4NetLoggingBuilder` that extension packages can chain off. `Quilt4Net.Toolkit.Api` adds `.AddHttpRequestLogging()` to enable HTTP request/response middleware including the `X-Correlation-ID` propagation scope:

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();
```

When `Quilt4Net.Toolkit.Api`'s `CorrelationIdMiddleware` is active, every `ILogger` call inside a request is scoped with the correlation id. That id reaches `customDimensions["CorrelationId"]` only when scope capture is enabled — set `IncludeScopes` (see below). See the Api package README.

### Exception data and log scopes

Two opt-in enrichers copy extra context onto the per-record `customDimensions`:

| Option | Default | Effect |
|---|---|---|
| `EnrichExceptionData` | **on** | Copies a logged exception's `Exception.Data` entries onto the exception telemetry. An id attached with `e.AddData("CorrelationId", guid)` becomes queryable in AI. |
| `IncludeScopes` | **off** | Captures `ILogger` scope values (e.g. the `CorrelationId` scope `CorrelationIdMiddleware` pushes) and copies them onto every record so they land in `customDimensions`. Opt-in because scope capture adds telemetry volume. |

```csharp
builder.AddQuilt4NetLogging(o =>
{
    o.IncludeScopes = true;      // scope values (incl. CorrelationId) → customDimensions
    // o.EnrichExceptionData = false;  // to opt out of Exception.Data enrichment
});
```

With `EnrichExceptionData` on, an exception carrying a correlation id is findable directly:

```csharp
catch (Exception e)
{
    e.AddData("CorrelationId", correlationId);   // Quilt4Net.Toolkit.Features.Measure
    logger.LogError(e, e.Message);
}
```

```kql
AppExceptions | where tostring(customDimensions.CorrelationId) == "<guid>"
```

> This `CorrelationId` (an id that flows across service hops and into `customDimensions`) is distinct from the user-facing 6-character `IncidentId` shown in Log-view error messages.

> Enrichment runs on the OpenTelemetry pipeline. Apps ingesting via the classic Application Insights SDK on AI 3.x must export logs through `Azure.Monitor.OpenTelemetry` to benefit — AI 3.x does not ingest `ILogger` telemetry through the classic pipeline.

### Distinguishing multiple deployments of the same binary

If the same compiled service is deployed under multiple logical names (multi-tenant / multi-brand / blue-green / shadow) and `service.name` is the same across them, telemetry alone can't tell the deployments apart — `host.name` only disambiguates the *machine*, not the *variant*. Set `ServiceInstanceId` so each row carries the deployment-variant identity:

```csharp
builder.AddQuilt4NetLogging(o =>
{
    o.ServiceInstanceId = builder.Configuration["DeploymentVariant"]; // e.g. "Thargelion"
});
```

When set, the value lands on the OTel resource (`cloud_RoleInstance` in Application Insights) **and** on `customDimensions["service.instance.id"]` for every record, so KQL can split rows by variant without portal lookups:

```kql
AppTraces
| where AppRoleName == 'Eplicta.FortDocs.Server'
| extend variant = tostring(todynamic(Properties)['service.instance.id'])
| summarize count() by variant, host = tostring(todynamic(Properties)['host.name'])
```

The toolkit's startup line also surfaces the variant when set:

```
Quilt4Net startup: Eplicta.FortDocs.Server [Thargelion] v1.2.9.0 in CI
```

Resolution precedence if `ServiceInstanceId` isn't passed in code:

1. `OTEL_RESOURCE_ATTRIBUTES` env var, parsed for the `service.instance.id=...` pair (the OTel-standard env var, [SDK env var spec](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)).
2. `QUILT4NET_SERVICE_INSTANCE_ID` env var (Quilt4Net shorthand for hosts that don't want to construct the multi-key OTel string by hand).
3. *Unset* — falls back to today's behaviour (`cloud_RoleInstance` is `MachineName`; no per-record attribute).

### Registration order with other ILoggerProvider registrations

If you also use a non-OTel logger provider — e.g. `Microsoft.ApplicationInsights.AspNetCore`'s `AddApplicationInsightsTelemetry` — **and** wrap `ILoggerFactory` (e.g. for enrichment), call `AddQuilt4NetLogging()` **before** the other AI/OTel `ILoggerProvider` registration **and** before the factory wrap. Some shapes of "wrap that captures `sp.GetServices<ILoggerProvider>()` and rebuilds a `LoggerFactory`" interact with the OTel pipeline in a way that silently drops `AppTraces` when the order is reversed (`AppRequests` continue to flow because they're written via `TelemetryClient.TrackRequest` directly). Tracked in [issue #87](https://github.com/Quilt4/Quilt4Net.Toolkit/issues/87).

The recommended shape:

```csharp
// 1. Quilt4Net first.
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();

// 2. Then the AI / other OTel provider.
builder.Services.AddApplicationInsightsTelemetry(o => { o.ConnectionString = "..."; });

// 3. Then any custom ILoggerFactory wrapping.
```

### Host and runtime metrics

`AddQuilt4NetLogging()` covers telemetry identity, logs, and traces — it does **not** emit host or process **metrics**. Host metrics (CPU, memory, disk space, network) belong to an OpenTelemetry Collector; .NET runtime/process metrics are available via standard OpenTelemetry instrumentation you can wire up yourself. See the docs article [Host and runtime metrics](../docs/articles/telemetry-identity.md#host-and-runtime-metrics) for how.

## Measure extensions

Extension methods on `ILogger` to measure and log execution time.

```csharp
// Measure synchronous work
_logger.Measure("ProcessOrder", () =>
{
    // work to measure
});

// Measure async work with result
var result = await _logger.MeasureAsync("FetchData", async () =>
{
    return await _repository.GetDataAsync();
});

// Log a count
_logger.Count("ItemsProcessed", items.Length);
```

## Logging attributes

Control HTTP request/response logging on endpoints.

```csharp
[Logging(RequestBody = true, ResponseBody = false)]
public async Task<IActionResult> StreamData() { ... }

[LoggingStream] // Shorthand for ResponseBody = false
public async Task<IActionResult> StreamEvents() { ... }
```

## Configuration

All options can be set via code or `appsettings.json`. Code takes priority.

```json
{
  "Quilt4Net": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "HealthClient": {
      "HealthAddress": "https://my-service.example.com/api/Health/"
    },
    "ApplicationInsights": {
      "TenantId": "your-tenant-id",
      "WorkspaceId": "your-workspace-id",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "AuthMode": "ClientSecret"
    },
    "RemoteConfiguration": {
      "Ttl": "00:10:00"
    },
    "Content": {
      "Application": "MyApp"
    }
  }
}
```
