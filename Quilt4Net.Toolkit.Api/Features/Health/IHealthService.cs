namespace Quilt4Net.Toolkit.Api.Features.Health;

public interface IHealthService
{
    Task<HealthResponse> GetStatusAsync(CancellationToken cancellationToken);
}