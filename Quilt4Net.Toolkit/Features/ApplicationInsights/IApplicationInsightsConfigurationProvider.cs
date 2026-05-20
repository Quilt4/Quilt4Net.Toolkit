namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Source of Application Insights configurations for the toolkit. Registered by
/// <c>AddQuilt4NetApplicationInsightsClientRemote</c>; consumed by Blazor components
/// that need to pick a configuration at render time (or render a selector when there
/// is more than one).
/// </summary>
public interface IApplicationInsightsConfigurationProvider
{
    /// <summary>
    /// Get all Application Insights configurations available to the calling team.
    /// Implementations cache and use stale-while-revalidate so a transient server outage
    /// does not break the consuming page.
    /// </summary>
    Task<ApplicationInsightsConfigurationResponse[]> GetAllAsync(CancellationToken cancellationToken = default);
}
