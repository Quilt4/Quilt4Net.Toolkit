using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Version.
/// </summary>
public record VersionResponse
{
    /// <summary>
    /// Version number.
    /// </summary>
    /// <example>1.0.0.0</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Version { get; init; }

    /// <summary>
    /// Name of the machine where the application is running.
    /// </summary>
    /// <example>Jupiter</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Machine { get; init; }

    /// <summary>
    /// Environment for the application.
    /// </summary>
    /// <example>Production</example>
    public required string Environment { get; init; }

    /// <summary>
    /// Public IP-address for the application.
    /// </summary>
    /// <example>127.0.0.1</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string IpAddress { get; init; }

    /// <summary>
    /// The process runs with 64 bit.
    /// </summary>
    public required bool Is64BitProcess { get; init; }
}