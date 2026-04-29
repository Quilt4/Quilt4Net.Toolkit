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
}
