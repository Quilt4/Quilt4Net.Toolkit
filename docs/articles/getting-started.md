# Getting started

## Install

```
dotnet add package Quilt4Net.Toolkit
```

For the Blazor UI components also add:

```
dotnet add package Quilt4Net.Toolkit.Blazor
```

## Register the Application Insights client

```csharp
builder.AddQuilt4NetApplicationInsightsClient();
```

That single call wires up:

- `IApplicationInsightsService` — KQL queries against your Log Analytics workspace
- `IVersionMatrixService` — cached app-version-per-environment view used by the version matrix component
- A configured response cache so repeat queries don't re-bill the workspace

## Configure auth + workspace

In `appsettings.json` under `Quilt4Net:ApplicationInsights`:

```json
{
  "Quilt4Net": {
    "ApplicationInsights": {
      "WorkspaceId": "<log-analytics-workspace-id>",
      "AuthMode": "DefaultAzureCredential"
    }
  }
}
```

Three auth modes are supported:

| Mode | When to use |
|---|---|
| `ClientSecret` | App registration with client id + secret. Required field: `TenantId`, `ClientId`, `ClientSecret`. |
| `ManagedIdentity` | Running in Azure with a managed identity that has Log Analytics Reader on the workspace. Optional `ClientId` for user-assigned identity. |
| `DefaultAzureCredential` | One config that works locally (via `az login`) and in Azure (via Managed Identity). Use this unless you have a reason not to. |

## First query

```csharp
public class WhatsLatest
{
    private readonly IApplicationInsightsService _ai;

    public WhatsLatest(IApplicationInsightsService ai) => _ai = ai;

    public async Task PrintAsync(IApplicationInsightsContext ctx)
    {
        await foreach (var cell in _ai.GetVersionMatrixAsync(ctx, lookback: TimeSpan.FromDays(7)))
        {
            Console.WriteLine($"{cell.ApplicationName} @ {cell.Environment}: {cell.Version} ({cell.LastSeen:u})");
        }
    }
}
```

`IApplicationInsightsContext` is anything carrying a `WorkspaceId` (and per-tenant overrides if you have them). Use `ApplicationInsightsContextExtensions.Current` for the `Quilt4Net:ApplicationInsights` config you wired up above.

## Where next

- **[Version matrix](version-matrix.md)** — render the same data as a Blazor table with optional alias folding and conflict detection
- **[API reference](xref:Quilt4Net.Toolkit)** — every public type and option
