using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Live;

/// <summary>
/// Response for Live.
/// </summary>
public record LiveResponse : ResponseBase<LiveStatus>
{
    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override required LiveStatus Status { get; init; }
}