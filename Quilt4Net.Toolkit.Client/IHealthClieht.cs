using Quilt4Net.Toolkit.Health;
using Quilt4Net.Toolkit.Live;
using Quilt4Net.Toolkit.Metrics;
using Quilt4Net.Toolkit.Ready;
using Quilt4Net.Toolkit.Version;

namespace Quilt4Net.Toolkit.Client;

public interface IHealthClieht
{
    Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken);
    Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken);
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}