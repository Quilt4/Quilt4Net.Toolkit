namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsContext
{
    public string TenantId { get; }
    public string WorkspaceId { get; }
    public string ClientId { get; }
    public string ClientSecret { get; }
}