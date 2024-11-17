using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

/// <summary>
/// Response for Ready.
/// </summary>
public record ReadyResponse
{
    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReadyStatus Status { get; init; }

    /// <summary>
    /// Components that have been checked.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, Component> Components { get; init; }
}