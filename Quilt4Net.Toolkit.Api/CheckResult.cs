namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Result of component check
/// </summary>
public record CheckResult
{
    /// <summary>
    /// True if the check was successful.
    /// </summary>
    public required bool Success { get; init; }
}