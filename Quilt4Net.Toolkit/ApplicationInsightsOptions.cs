using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit;

/// <summary>
/// This option can be configured by code or with appsettings.json on location "Quilt4Net/ApplicationInsights"
/// </summary>
public record ApplicationInsightsOptions
{
    /// <summary>
    /// This value can be found under 'Tenant properties' in Azure portal. Only required when <see cref="AuthMode"/> is <see cref="ApplicationInsightsAuthMode.ClientSecret"/>.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// This value can be found under application insights. Click 'Workspace' under overview.
    /// </summary>
    public string WorkspaceId { get; set; }

    /// <summary>
    /// For <see cref="ApplicationInsightsAuthMode.ClientSecret"/>: search for 'App registrations' in Azure portal or create a new App registration for Application Insights access. (Remember the app-name)
    /// When creating a new app registration, assign the API permission 'Application Insights API' with 'Data.Read' access. (Add a permission / APIs my organization uses / Application Insights API) (Use Application permission, not Delegated)
    /// Also create a Client secret that will be used for 'ClientSecret' below.
    /// Under workspace on your application insights, go to 'Access control (IAM)' and add role access 'Reader' to the app registration with app-name.
    ///
    /// For <see cref="ApplicationInsightsAuthMode.ManagedIdentity"/>: leave empty to use the system-assigned managed identity, or set to the client id of a user-assigned managed identity.
    ///
    /// For <see cref="ApplicationInsightsAuthMode.DefaultAzureCredential"/>: optional hint — when set, the chained credential prefers this user-assigned managed identity if Managed Identity lights up in the chain.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Only required when <see cref="AuthMode"/> is <see cref="ApplicationInsightsAuthMode.ClientSecret"/>. Not consulted in <see cref="ApplicationInsightsAuthMode.ManagedIdentity"/> or <see cref="ApplicationInsightsAuthMode.DefaultAzureCredential"/>.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Authentication mode for connecting to the Application Insights / Log Analytics workspace.
    /// Defaults to <see cref="ApplicationInsightsAuthMode.ClientSecret"/>.
    /// Use <see cref="ApplicationInsightsAuthMode.ManagedIdentity"/> when running in Azure and the hosting identity has Log Analytics Reader on the workspace.
    /// Use <see cref="ApplicationInsightsAuthMode.DefaultAzureCredential"/> for a chained credential that works locally (via <c>az login</c>) and in Azure (via Managed Identity) with the same configuration.
    /// </summary>
    public ApplicationInsightsAuthMode AuthMode { get; set; } = ApplicationInsightsAuthMode.ClientSecret;
}