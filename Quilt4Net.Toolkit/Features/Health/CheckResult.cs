namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Result of component check
/// </summary>
public record CheckResult
{
    /// <summary>
    /// True if the check was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Optional message information that will be shown in the response.
    /// </summary>
    public string Message { get; init; }
}