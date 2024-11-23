using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

/// <summary>
/// Service for Metrics.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Get metrics information.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}