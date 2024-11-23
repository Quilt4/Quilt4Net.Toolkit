using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Live.
/// </summary>
public record LiveResponse : ResponseBase<LiveStatus>
{
    internal LiveResponse()
    {
    }

    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override required LiveStatus Status { get; init; }
}