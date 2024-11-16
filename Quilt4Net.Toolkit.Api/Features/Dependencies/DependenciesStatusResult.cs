namespace Quilt4Net.Toolkit.Api.Features.Dependencies;

public enum DependenciesStatusResult
{
    /// <summary>
    /// The dependency is operational.
    /// </summary>
    Healthy,

    /// <summary>
    /// The dependency is experiencing issues (e.g., increased latency, partial availability).
    /// </summary>
    Degraded,

    /// <summary>
    /// The dependency is non-functional or unavailable.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// The status of the dependency could not be determined (e.g., a timeout occurred).
    /// </summary>
    Unknown
}