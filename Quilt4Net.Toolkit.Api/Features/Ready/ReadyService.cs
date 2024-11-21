using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Framework;
using Quilt4Net.Toolkit.Ready;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

internal class ReadyService : IReadyService
{
    private readonly IHealthService _healthService;

    public ReadyService(IHealthService healthService)
    {
        _healthService = healthService;
    }

    public async Task<ReadyResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var result = await _healthService.GetStatusAsync(cancellationToken);

        return new ReadyResponse
        {
            Status = result.Status.ToReadyStatusResult(),
            Components = result.Components.ToDictionary(x => x.Key, x => new Quilt4Net.Toolkit.Ready.Component
            {
                Status = x.Value.Status.ToReadyStatusResult()
            })
        };
    }
}