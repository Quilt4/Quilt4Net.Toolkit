namespace Quilt4Net.Toolkit;

/// <summary>
///
/// </summary>
public record Quilt4NetApplicationInsightsOptions
{
    /// <summary>
    /// This value can be found under 'Tennant properties' in Azure portal.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// This value can be dound under application insights. Click 'Workspace' under overview.
    /// </summary>
    public string WorkspaceId { get; set; }

    /// <summary>
    /// Search for 'App registrations' in Azure portal or create a new App registration for Application Insights access.
    /// When creating a new app registration, assign the API permission 'Application Insights API' with 'Data.Read' access. Also create a Client secret.
    /// Under workspace go to 'Access control (IAM)' and add role access 'Reader' to the app registration.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Secret 
    /// </summary>
    public string ClientSecret { get; set; }
}