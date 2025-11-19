using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Features.Health;
using System.Runtime.CompilerServices;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

/// <summary>
/// Service for Ready.
/// </summary>
public interface IReadyService
{
    /// <summary>
    /// Performs Ready checks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<KeyValuePair<string, ReadyComponent>> GetStatusAsync(CancellationToken cancellationToken);
}

internal class ReadyService : IReadyService
{
    private readonly IHealthService _healthService;

    public ReadyService(IHealthService healthService)
    {
        _healthService = healthService;
    }

    public async IAsyncEnumerable<KeyValuePair<string, ReadyComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var variable in _healthService.GetStatusAsync(_ => true, false, cancellationToken))
        {
            yield return new KeyValuePair<string, ReadyComponent>(variable.Key, new ReadyComponent { Status = variable.Value.Status.ToReadyStatusResult() });
        }
    }
}

internal static class StatusConverter
{
    public static ReadyStatus ToReadyStatusResult(this HealthStatus healthStatus)
    {
        switch (healthStatus)
        {
            case HealthStatus.Healthy:
                return ReadyStatus.Ready;
            case HealthStatus.Degraded:
                return ReadyStatus.Degraded;
            case HealthStatus.Unhealthy:
                return ReadyStatus.Unready;
            default:
                throw new ArgumentOutOfRangeException(nameof(healthStatus), healthStatus, null);
        }
    }
}