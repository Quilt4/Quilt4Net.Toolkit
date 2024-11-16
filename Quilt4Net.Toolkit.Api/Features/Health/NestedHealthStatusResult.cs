namespace Quilt4Net.Toolkit.Api.Features.Health;

public enum NestedHealthStatusResult
{
    /// <summary>
    /// The specific component is working correctly.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component is experiencing issues but is still operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// The component is non-functional or unavailable.
    /// </summary>
    Unhealthy
}