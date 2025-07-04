using System.Runtime.CompilerServices;
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

    public async IAsyncEnumerable<KeyValuePair<string, ReadyComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //await foreach (var variable in _healthService.GetStatusAsync(x => x.NeededToBeReady ?? x.Essential, false, cancellationToken))
        await foreach (var variable in _healthService.GetStatusAsync(_ => true, false, cancellationToken))
        {
            yield return new KeyValuePair<string, ReadyComponent>(variable.Key, new ReadyComponent { Status = variable.Value.Status.ToReadyStatusResult() });
        }
    }
}