namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Component for availability checking.
/// </summary>
public record Component
{
    /// <summary>
    /// Name of the component. This name needs to be unique.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Non-essential component will be considered Degraded if they fail.
    /// Essential components will be considered to Unhealthy/Unready that will result in 503 response.
    /// Default is true.
    /// </summary>
    public bool Essential { get; init; } = true;

    ///// <summary>
    ///// Used by Readyness to check if a component is needed for the system to be considered to be ready.
    ///// By default, the parameter Essential is used do determine if the component is needed to be considered to be ready.
    ///// This value can be set to true or false to override the Essential parameter.
    ///// </summary>
    //public bool? NeededToBeReady { get; init; }

    /// <summary>
    /// Method that performs the check for the component.
    /// </summary>
    public required Func<IServiceProvider, Task<CheckResult>> CheckAsync { get; init; }
}