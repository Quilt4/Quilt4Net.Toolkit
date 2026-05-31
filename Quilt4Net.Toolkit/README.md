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
var maxRetries = await _configService.GetValueAsync("MaxRetries", fallback: 3);
```

### RemoteConfigurationOptions

| Property | Default | Description |
|----------|---------|-------------|
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `Ttl` | `null` | Time-to-live for cached values. |
| `StaleWhileRevalidate` | `true` | When `true`, an expired value is returned immediately and refreshed in the background. Set `false` to refresh synchronously so callers always get a fresh value (subject to `HttpTimeout`). |

Configuration path: `Quilt4Net:RemoteConfiguration`

## Content management

Manage multilingual content from [Quilt4Net Web](https://quilt4net.com).

```csharp
builder.AddQuilt4NetContent();
```

Inject `IContentService` to retrieve and manage content.

```csharp
var (value, success) = await _contentService.GetContentAsync("welcome-message", "Hello!", languageKey, ContentFormat.String);
```

### ContentOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Application` | Assembly name | Application name. |
| `Quilt4NetAddress` | `"https://quilt4net.com/"` | Quilt4Net server address. |
| `ApiKey` | `null` | API key from [Quilt4Net Web](https://quilt4net.com). |
| `StaleWhileRevalidate` | `true` | When `true`, an expired value is returned immediately and refreshed in the background. Set `false` to refresh synchronously so callers always get a fresh value (subject to `HttpTimeout`). |

Configuration path: `Quilt4Net:Content`

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

The remote provider caches the configuration list per the `RemoteConfigurationOptions.Ttl` (default 5 min) with stale-while-revalidate, so transient server outages don't break the consuming page. When more than one workspace is configured on the server for the team, every workspace is reachable; the Blazor `LogView` renders an in-component **dropdown** (one workspace) and `VersionMatrixDisplay` a **multi-select radio bar** that merges the matrix across the selected workspaces — selecting none shows all (see [Quilt4Net.Toolkit.Blazor README](https://github.com/Quilt4/Quilt4Net.Toolkit/blob/master/Quilt4Net.Toolkit.Blazor/README.md)).

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
    o.GroupId = "507f1f77bcf86cd799439011";  // the group's ObjectId from the admin UI
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
| `ApiKey` | — *(required)* | The API key minted for this Value Group in the server's admin UI. Must carry the `valuegroup:read` scope and be tag-bound to `GroupId`. |
| `GroupId` | — *(required)* | The id of the Value Group this client fetches. |
| `Ttl` | `5 min` | Cache freshness window. Subsequent calls within the window serve the cached bundle. |
| `HttpTimeout` | `5 s` | HTTP timeout. On timeout the cached bundle is served if available. |

Configuration path: `Quilt4Net:ValueGroup` (or top-level `Quilt4Net:ApiKey` + `Quilt4Net:Quilt4NetAddress` for shared keys).

### Behaviour and contract

- **Stale-while-revalidate**: returning a fresh bundle is the default. Stale-cache fallback applies on transient HTTP errors *and* on timeout.
- **`ValueGroupAuthorizationException` on 401/403**: a revoked key or wrong-binding response *throws*, by design. Unlike `IRemoteConfigurationService` (which silently serves fallback values), Value Groups carry secret-bearing data, so the consumer must learn it has been revoked rather than continue using cached secrets.
- **One client = one group**: register multiple clients via keyed services if the consumer needs more than one group.

### Minting a key

In the Quilt4Net.Server admin UI under **Value Groups**: select the group → API Keys panel → **Mint new key**. The raw key is shown exactly once — save it immediately. The key carries only the `valuegroup:read` scope and is tag-bound on the server side to this one group; it cannot reach any other team data.

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

When `Quilt4Net.Toolkit.Api`'s `CorrelationIdMiddleware` is active, every `ILogger` call inside a request also picks up `customDimensions["CorrelationId"]` automatically — see the Api package README.

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
