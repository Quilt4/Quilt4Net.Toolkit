# Quilt4Net.Toolkit.Api
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

HTTP request/response logging and correlation tracking middleware for .NET Web Applications.

Captures method, path, headers, query parameters, request/response bodies, client IP, and execution time. Logs to Application Insights and/or standard `ILogger`.

## Get started

Install the NuGet package [Quilt4Net.Toolkit.Api](https://www.nuget.org/packages/Quilt4Net.Toolkit.Api) and register the service in `Program.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();

var app = builder.Build();

app.UseQuilt4NetLogging();

app.Run();
```

`AddQuilt4NetLogging()` registers an `ITelemetryInitializer` that tags all Application Insights telemetry (traces, exceptions, requests) with `Cloud.RoleName`, `Component.Version`, and `Environment`. `AddHttpRequestLogging()` opts in to HTTP request/response middleware. By default, all requests to paths starting with `/Api` are logged.

> The old `AddQuilt4NetApiLogging()` and `UseQuilt4NetApiLogging()` methods still work but are deprecated.

## Correlation ID

When `UseCorrelationId` is enabled (default), the middleware reads the `X-Correlation-ID` header from incoming requests. If no header is present, a new GUID is generated. The correlation ID is stored in `HttpContext.Items` and returned in the response header, enabling distributed tracing across services.

## Logging mode

Control where logs are sent using `HttpRequestLogMode`.

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging(o =>
    {
        o.LogHttpRequest = HttpRequestLogMode.ApplicationInsights | HttpRequestLogMode.Logger;
    });
```

| Value | Description |
|-------|-------------|
| `None` | No logging. |
| `ApplicationInsights` | Append request/response data to Application Insights request telemetry. |
| `Logger` | Log via the standard `ILogger` pipeline. |

Values can be combined with `|` to log to multiple destinations.

## Path filtering

By default, only paths matching `^/Api` (case-insensitive) are logged. Override with regex patterns.

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging(o =>
    {
        o.IncludePaths = [".*"]; // Log all paths
    });
```

## Per-endpoint control

Use `[Logging]` and `[LoggingStream]` attributes to override logging behavior on individual endpoints.

```csharp
[Logging(RequestBody = true, ResponseBody = false)]
public async Task<IActionResult> StreamData() { ... }

[Logging(Enabled = false)]
public IActionResult InternalEndpoint() { ... }

[LoggingStream] // Shorthand for ResponseBody = false
public async Task<IActionResult> StreamEvents() { ... }
```

The `[Logging]` attribute can be applied to methods or classes.

| Property | Default | Description |
|----------|---------|-------------|
| `Enabled` | `true` | Enable or disable all logging for the endpoint. |
| `RequestBody` | `true` | Log the request body. |
| `ResponseBody` | `true` | Log the response body. Set to `false` for streaming endpoints. |

## Interceptor

Use an interceptor to modify or filter logged data before it is written. This is useful for removing sensitive information such as passwords or API keys.

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging(o =>
    {
        o.Interceptor = async (request, response, properties, serviceProvider) =>
        {
            // Remove sensitive headers
            request.Headers.Remove("Authorization");
            return (request, response, properties);
        };
    });
```

## Configuration

All options can be set via code or `appsettings.json`. Code takes priority over `appsettings.json`, which takes priority over defaults.

### Code configuration

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging(o =>
    {
        o.LogHttpRequest = HttpRequestLogMode.ApplicationInsights;
        o.UseCorrelationId = true;
        o.MaxBodySize = 5_000_000;
        o.IncludePaths = ["^/Api", "^/webhook"];
        o.LogRequestBodyByDefault = true;
        o.LogResponseBodyByDefault = false;
    });
```

### appsettings.json

```json
{
  "Quilt4Net": {
    "ApiLogging": {
      "LogHttpRequest": 1,
      "UseCorrelationId": true,
      "MonitorName": "Quilt4Net",
      "MaxBodySize": 1000000,
      "IncludePaths": ["^/Api"],
      "LogRequestBodyByDefault": true,
      "LogResponseBodyByDefault": false
    }
  }
}
```

Configuration path: `Quilt4Net:ApiLogging`

### LoggingOptions

| Property | Default | Description |
|----------|---------|-------------|
| `LogHttpRequest` | `ApplicationInsights` | Logging destination. Combine with `\|` for multiple. |
| `UseCorrelationId` | `true` | Enable `X-Correlation-ID` header tracking. |
| `MonitorName` | `"Quilt4Net"` | Monitor name for tracking log items. Set to empty to omit. |
| `MaxBodySize` | `1 MB` | Maximum body size to log. Set to `0` to disable body logging. |
| `IncludePaths` | `["^/Api"]` | Regex patterns (case-insensitive) for paths to include. |
| `LogRequestBodyByDefault` | `true` | Log request body by default. Override per endpoint with `[Logging]`. |
| `LogResponseBodyByDefault` | `false` | Log response body by default. Override per endpoint with `[Logging]`. |
| `Interceptor` | `null` | Callback to modify or filter logged data before writing. |

## Logged data

### Request

| Field | Description |
|-------|-------------|
| `Method` | HTTP method (GET, POST, etc.). |
| `Path` | Request path. |
| `Headers` | Request headers (cookies are automatically filtered). |
| `Query` | Query string parameters. |
| `Body` | Request body (respects `MaxBodySize` limit). |
| `ClientIp` | Client IP address. |

### Response

| Field | Description |
|-------|-------------|
| `StatusCode` | HTTP status code. |
| `Headers` | Response headers. |
| `Body` | Response body (respects `MaxBodySize` limit). |
