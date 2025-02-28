namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Version.
/// </summary>
public record VersionResponse
{
    /// <summary>
    /// Version number.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Name of the machine where the application is running.
    /// </summary>
    public required string Machine { get; init; }

    /// <summary>
    /// Environment for the application.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Public IP-address for the application.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// The process runs with 64 bit.
    /// </summary>
    public required bool Is64BitProcess { get; init; }
}