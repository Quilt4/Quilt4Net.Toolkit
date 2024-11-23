using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Ready;

/// <summary>
/// Component for Ready check.
/// </summary>
public record Component
{
    /// <summary>
    /// Status for the component check.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReadyStatus Status { get; init; }
}