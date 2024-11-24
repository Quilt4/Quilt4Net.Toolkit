namespace Quilt4Net.Toolkit;

public record Quilt4NetApplicationInsightsOptions
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string WorkspaceId { get; set; }
}