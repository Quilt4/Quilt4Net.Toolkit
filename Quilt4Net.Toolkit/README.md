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

Client for querying Application Insights data (logs, metrics, exceptions).

```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```

### ApplicationInsightsOptions

| Property | Default | Description |
|----------|---------|-------------|
| `TenantId` | `null` | Azure AD tenant ID (found under "Tenant properties" in Azure portal). Only required when `AuthMode = ClientSecret`. |
| `WorkspaceId` | `null` | Application Insights workspace ID. |
| `ClientId` | `null` | For `ClientSecret`: app registration client ID with `Data.Read` permission on Application Insights API. For `ManagedIdentity`: empty for system-assigned MI, or the user-assigned MI's client ID. |
| `ClientSecret` | `null` | Client secret for the app registration. Only required when `AuthMode = ClientSecret`. |
| `AuthMode` | `ClientSecret` | Authentication mode: `ClientSecret` (service principal) or `ManagedIdentity` (Azure-hosted apps). |

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

## Universal telemetry tagging

`AddQuilt4NetLogging()` registers an `ITelemetryInitializer` that tags every Application Insights telemetry item (traces, exceptions, requests, dependencies) with application identity. Works for all app types — Web API, Blazor, WPF, console, worker service.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQuilt4NetLogging();
```

By default the initializer sets:

| Telemetry property | Default value |
|---|---|
| `Cloud.RoleName` | `IHostEnvironment.ApplicationName` → entry assembly name (framework assemblies excluded) |
| `Component.Version` | Application assembly version |
| `GlobalProperties["Environment"]` | `IHostEnvironment.EnvironmentName` → `DOTNET_ENVIRONMENT` → `ASPNETCORE_ENVIRONMENT` → `"Production"` |

Override via callback or `appsettings.json`:

```csharp
builder.AddQuilt4NetLogging(o =>
{
    o.ApplicationName = "florida-server";
    o.Version = "2.0.0";
    o.Environment = "Production";
});
```

```json
{
  "Quilt4Net": {
    "Logging": {
      "ApplicationName": "florida-server",
      "Version": "2.0.0",
      "Environment": "Production"
    }
  }
}
```

`AddQuilt4NetLogging()` returns a `Quilt4NetLoggingBuilder` that extension packages can chain off. For example, `Quilt4Net.Toolkit.Api` adds `.AddHttpRequestLogging()` to enable HTTP request/response middleware:

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();
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
