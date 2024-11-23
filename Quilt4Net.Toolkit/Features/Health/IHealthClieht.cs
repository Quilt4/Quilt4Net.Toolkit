namespace Quilt4Net.Toolkit.Features.Health;

public interface IHealthClieht
{
    Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken);
    Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken);
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken);
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}