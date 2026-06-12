using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Version.
/// </summary>
public record VersionResponse
{
    /// <summary>
    /// Numeric four-part assembly version (<see cref="System.Reflection.AssemblyName.Version"/>).
    /// Pre-release SemVer tags are stripped here — use <see cref="InformationalVersion"/> when
    /// the build's <c>-pre.n</c> / SemVer identity matters (e.g. post-deploy verification).
    /// </summary>
    /// <example>1.0.0.0</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Version { get; init; }

    /// <summary>
    /// Informational / SemVer version sourced from
    /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>, including any
    /// pre-release tag. SourceLink build metadata (anything after <c>+</c>) is stripped.
    /// Null when the attribute is absent on the entry assembly.
    /// </summary>
    /// <example>1.0.0-pre.1</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string InformationalVersion { get; init; }

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