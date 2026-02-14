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
builder.Services.AddQuilt4NetRemoteConfiguration();
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
builder.Services.AddQuilt4NetContent();
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
builder.Services.AddQuilt4NetHealthClient(o =>
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
builder.Services.AddQuilt4NetApplicationInsightsClient();
```

### ApplicationInsightsOptions

| Property | Default | Description |
|----------|---------|-------------|
| `TenantId` | `null` | Azure AD tenant ID (found under "Tenant properties" in Azure portal). |
| `WorkspaceId` | `null` | Application Insights workspace ID. |
| `ClientId` | `null` | App registration client ID with `Data.Read` permission on Application Insights API. |
| `ClientSecret` | `null` | Client secret for the app registration. |

Configuration path: `Quilt4Net:ApplicationInsights`

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
      "ClientSecret": "your-client-secret"
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
