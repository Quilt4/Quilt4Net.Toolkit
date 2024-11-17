namespace Quilt4Net.Toolkit.Api.Features.Health;

/// <summary>
/// Status for Health.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// All components are operational, and the service is running as expected.
    /// </summary>
    Healthy,

    /// <summary>
    /// The service is operational but with reduced performance or reliability in one or more components.
    /// </summary>
    Degraded,

    /// <summary>
    /// The service or critical components are non-functional, and the application cannot serve traffic reliably.
    /// </summary>
    Unhealthy,

    ///// <summary>
    ///// The service is intentionally under maintenance and may not perform all operations.
    ///// </summary>
    //Maintenance
}