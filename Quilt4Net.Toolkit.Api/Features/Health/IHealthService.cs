using Quilt4Net.Toolkit.Health;

namespace Quilt4Net.Toolkit.Api.Features.Health;

/// <summary>
/// Service for Health.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Performs Health checks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<HealthResponse> GetStatusAsync(CancellationToken cancellationToken);
}