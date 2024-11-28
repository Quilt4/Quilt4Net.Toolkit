using Quilt4Net.Toolkit.Features.Health;

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
    IAsyncEnumerable<KeyValuePair<string, HealthComponent>> GetStatusAsync(CancellationToken cancellationToken);
}