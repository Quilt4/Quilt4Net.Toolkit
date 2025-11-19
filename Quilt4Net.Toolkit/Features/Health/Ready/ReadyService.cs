using System.Runtime.CompilerServices;

namespace Quilt4Net.Toolkit.Features.Health.Ready;

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