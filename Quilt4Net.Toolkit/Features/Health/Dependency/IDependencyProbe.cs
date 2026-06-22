namespace Quilt4Net.Toolkit.Features.Health.Dependency;

internal interface IDependencyProbe
{
    /// <summary>
    /// Probes a single dependency's health endpoint. Never throws for a non-success response or a
    /// transport/parse failure — those are reported as an unhealthy/degraded <see cref="HealthResponse"/>.
    /// Only cancellation propagates.
    /// </summary>
    Task<HealthResponse> ProbeAsync(Dependency dependency, CancellationToken cancellationToken);
}
