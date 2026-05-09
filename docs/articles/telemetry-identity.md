# Telemetry identity & correlation

`AddQuilt4NetLogging()` does two things:

1. Configures **OpenTelemetry resource attributes** so the Azure Monitor exporter populates the AI columns `cloud_RoleName`, `application_Version`, and `cloud_RoleInstance`.
2. Registers **two `BaseProcessor`s** — one for `LogRecord`, one for `Activity` — that copy a five-attribute identity onto every per-record `Properties` bag at export time. The Azure Monitor exporter does *not* forward arbitrary OTel resource attributes to per-row Properties for log records, so without this step `customDimensions["deployment.environment"]` would always be empty.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddQuilt4NetLogging();
```

## What lands on every record

Five attributes attached to every `AppTrace`, `AppException`, `AppRequest`, and outbound `AppDependency`:

| Key | Default | Notes |
|---|---|---|
| `service.name` | `IHostEnvironment.ApplicationName` | Also surfaces as `cloud_RoleName`. |
| `service.version` | Entry assembly version | Also surfaces as `application_Version`. |
| `host.name` | `Environment.MachineName` | Also surfaces as `cloud_RoleInstance`. |
| `deployment.environment` | `IHostEnvironment.EnvironmentName` → `DOTNET_ENVIRONMENT` → `ASPNETCORE_ENVIRONMENT` → `"Production"` | Per-record copy. Read-side queries use a centralised KQL projection that coalesces this with the legacy `AspNetCoreEnvironment` scope tag. |
| `quilt4net.monitor` | `"Quilt4Net"` (configurable via [`MonitorName`](xref:Quilt4Net.Toolkit.Quilt4NetLoggingOptions.MonitorName)) | Identifies the instrumentation source — distinguishes telemetry from multiple Quilt4Net-instrumented services shipping to the same workspace. |

## Override

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

## Correlation across requests + handlers

Pair `AddQuilt4NetLogging()` with `Quilt4Net.Toolkit.Api`:

```csharp
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();

var app = builder.Build();
app.UseQuilt4NetLogging();
```

`CorrelationIdMiddleware`:

1. Reads `X-Correlation-ID` from the request, or generates a new GUID.
2. Echoes it back as a response header.
3. Pushes it into a logging scope (`Logger.BeginScope({ ["CorrelationId"] = id })`) for the duration of the request.

Every `ILogger` call made while handling the request inherits the id as a structured property. The Azure Monitor exporter writes it to `customDimensions["CorrelationId"]` on every resulting `AppTrace` / `AppException` / `AppRequest`. KQL pattern:

```kql
union AppTraces, AppExceptions, AppRequests
| where Properties contains "<the-correlation-id>"
| order by TimeGenerated asc
```

A client that wants to chain calls just sends the same header to every server. Server code that wants to start a new chain can read `HttpContext.Items["CorrelationId"]` and forward it to outbound `HttpClient` calls.

## Demo endpoint

`Quilt4Net.Toolkit.Blazor.Server.Sample` ships a working endpoint:

```bash
curl -i -H "X-Correlation-ID: my-test-1" https://localhost:7187/api/correlation-demo
```

→ three `AppTrace` rows in AI, all sharing `customDimensions["CorrelationId"] == "my-test-1"`.

## Where next

- **[Log views](log-views.md)** — render and query the resulting telemetry.
- **API reference**: `xref:Quilt4Net.Toolkit.Quilt4NetLoggingOptions`, `xref:Quilt4Net.Toolkit.Features.Logging.TelemetryIdentityLogProcessor`.
