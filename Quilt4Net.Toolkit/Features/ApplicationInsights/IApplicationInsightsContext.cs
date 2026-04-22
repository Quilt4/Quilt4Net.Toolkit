namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsContext
{
    public string TenantId { get; }
    public string WorkspaceId { get; }
    public string ClientId { get; }
    public string ClientSecret { get; }

    /// <summary>
    /// Authentication mode used when connecting to the workspace. Defaults to <see cref="ApplicationInsightsAuthMode.ClientSecret"/>
    /// so existing implementers keep their current behaviour without needing to implement this member.
    /// </summary>
    public ApplicationInsightsAuthMode AuthMode => ApplicationInsightsAuthMode.ClientSecret;
}