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

    /// <summary>
    /// Method that performs the check for the component.
    /// </summary>
    public required Func<IServiceProvider, Task<CheckResult>> CheckAsync { get; init; }
}