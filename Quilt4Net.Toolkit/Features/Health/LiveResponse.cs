using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Live.
/// </summary>
public record LiveResponse : ResponseBase<LiveStatus>
{
    /// <summary>
    /// Overall status.
    /// </summary>
    /// <example>alive</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override required LiveStatus Status { get; init; }
}