namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IVersionMatrixService
{
    /// <summary>
    /// Get the version matrix for a workspace. Uses an in-memory cache keyed by (workspaceId, lookback).
    /// </summary>
    Task<VersionMatrixView> GetAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Force a fresh fetch from Application Insights, bypassing the in-memory cache.
    /// </summary>
    Task<VersionMatrixView> RefreshAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, CancellationToken cancellationToken = default);
}
