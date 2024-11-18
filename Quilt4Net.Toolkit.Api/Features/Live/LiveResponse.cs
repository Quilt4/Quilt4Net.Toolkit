using System.Text.Json.Serialization;
using Quilt4Net.Toolkit.Api.Features.Ready;

namespace Quilt4Net.Toolkit.Api.Features.Live;

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