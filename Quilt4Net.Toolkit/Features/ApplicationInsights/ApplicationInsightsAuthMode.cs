namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Authentication mode for querying an Application Insights / Log Analytics workspace.
/// </summary>
public enum ApplicationInsightsAuthMode
{
    /// <summary>
    /// Service principal with a client secret (TenantId + ClientId + ClientSecret).
    /// The default for backward compatibility.
    /// </summary>
    ClientSecret = 0,

    /// <summary>
    /// Azure Managed Identity. Works when the app runs in Azure (App Service, Container Apps, VMs, …)
    /// and its identity has been granted Log Analytics Reader (or Monitoring Reader) on the target workspace.
    /// If <c>ClientId</c> is set, a user-assigned managed identity is used; otherwise the system-assigned identity.
    /// </summary>
    ManagedIdentity = 1,

    /// <summary>
    /// Azure <c>DefaultAzureCredential</c> chain — falls through environment variables, workload identity,
    /// Managed Identity (in Azure), Visual Studio / VS Code credentials, and Azure CLI (<c>az login</c>),
    /// returning the first that succeeds. Same configuration works for local development (developer runs
    /// <c>az login</c> once) and for Azure-hosted deployments (system-assigned or user-assigned managed identity).
    /// <c>TenantId</c> and <c>ClientId</c> are forwarded as hints; both may be left empty.
    /// </summary>
    DefaultAzureCredential = 2,
}
