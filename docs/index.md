---
_layout: landing
---

# Quilt4Net.Toolkit

A toolkit for **.NET 8 / 9 / 10** that adds health checks, observability, remote configuration, feature toggles, content / language management, and a drop-in Application Insights UI to your application. Self-host the admin pieces or pair with [Quilt4Net Web](https://quilt4net.com).

## Packages

| Package | What it does |
|---|---|
| [Quilt4Net.Toolkit](https://www.nuget.org/packages/Quilt4Net.Toolkit) | Core: feature toggles, remote configuration, content/language services, Application Insights client, version matrix. |
| [Quilt4Net.Toolkit.Api](https://www.nuget.org/packages/Quilt4Net.Toolkit.Api) | Minimal-API endpoints exposing the toolkit's services to Blazor WASM clients and external integrators. |
| [Quilt4Net.Toolkit.Blazor](https://www.nuget.org/packages/Quilt4Net.Toolkit.Blazor) | Razor components: log viewer, version matrix, content/language admin, configuration toggles. |
| [Quilt4Net.Toolkit.Health](https://www.nuget.org/packages/Quilt4Net.Toolkit.Health) | Component-based health checks with metrics for memory, GPU, storage, and machine info. |
| [Quilt4Net.Toolkit.Mcp](https://www.nuget.org/packages/Quilt4Net.Toolkit.Mcp) | MCP (Model Context Protocol) provider — exposes Application Insights queries to AI clients. |

## Quick start

```
dotnet add package Quilt4Net.Toolkit
```

```csharp
builder.AddQuilt4NetApplicationInsightsClient(o =>
{
    o.WorkspaceId = "<log-analytics-workspace-id>";
    o.AuthMode = ApplicationInsightsAuthMode.DefaultAzureCredential;
});
```

Drop the version matrix into a Blazor page:

```razor
@using Quilt4Net.Toolkit.Blazor.Features.VersionMatrix
@using Quilt4Net.Toolkit.Features.ApplicationInsights

<VersionMatrixDisplay Context="@Context" />
```

## Where next

- **[Articles](articles/index.md)** — feature guides: getting started, version matrix, alias map, environment ordering
- **[API reference](xref:Quilt4Net.Toolkit)** — every public type, method, and option, generated from XML doc comments
- **[GitHub](https://github.com/Quilt4/Quilt4Net.Toolkit)** — source, issues, releases
