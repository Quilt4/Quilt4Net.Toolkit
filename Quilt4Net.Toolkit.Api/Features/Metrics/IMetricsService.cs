namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public interface IMetricsService
{
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}