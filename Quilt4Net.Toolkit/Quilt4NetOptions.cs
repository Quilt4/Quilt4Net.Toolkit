namespace Quilt4Net.Toolkit;

public record Quilt4NetOptions
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string WorkspaceId { get; set; }
    public Uri HealthAddress { get; set; }
}