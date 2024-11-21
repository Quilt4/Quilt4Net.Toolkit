using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Health;
using Quilt4Net.Toolkit.Live;
using Quilt4Net.Toolkit.Ready;

namespace Quilt4Net.Toolkit.Client;

public interface IHealthClieht
{
    Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken);
    Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken);
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}