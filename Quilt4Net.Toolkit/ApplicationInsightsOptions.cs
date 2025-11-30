namespace Quilt4Net.Toolkit;

/// <summary>
///
/// </summary>
public record ApplicationInsightsOptions
{
    /// <summary>
    /// This value can be found under 'Tennant properties' in Azure portal.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// This value can be found under application insights. Click 'Workspace' under overview.
    /// </summary>
    public string WorkspaceId { get; set; }

    /// <summary>
    /// Search for 'App registrations' in Azure portal or create a new App registration for Application Insights access. (Remember the app-name)
    /// When creating a new app registration, assign the API permission 'Application Insights API' with 'Data.Read' access. (Add a permission / APIs my organization uses / Application Insights API) (Use Application, not Delegated)
    /// Also create a Client secret that will be used for 'ClientSecret' below.
    /// Under workspace on your application insights, go to 'Access control (IAM)' and add role access 'Reader' to the app registration with app-name.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string ClientSecret { get; set; }
}