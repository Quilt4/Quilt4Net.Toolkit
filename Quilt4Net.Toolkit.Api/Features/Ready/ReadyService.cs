using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Framework;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

internal class ReadyService : IReadyService
{
    private readonly IHealthService _healthService;

    public ReadyService(IHealthService healthService)
    {
        _healthService = healthService;
    }

    public async IAsyncEnumerable<KeyValuePair<string, ReadyComponent>> GetStatusAsync(CancellationToken cancellationToken)
    {
        await foreach (var variable in _healthService.GetStatusAsync(cancellationToken))
        {
            yield return new KeyValuePair<string, ReadyComponent>(variable.Key, new ReadyComponent { Status = variable.Value.Status.ToReadyStatusResult() });
        }
    }
}