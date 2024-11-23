using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Component for Ready check.
/// </summary>
public record ReadyComponent
{
    internal ReadyComponent()
    {
    }

    /// <summary>
    /// Status for the component check.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReadyStatus Status { get; init; }
}