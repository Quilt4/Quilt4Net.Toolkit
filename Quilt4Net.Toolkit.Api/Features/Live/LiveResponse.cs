using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Live;

/// <summary>
/// Response for Live.
/// </summary>
public record LiveResponse
{
    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LiveStatus Status { get; init; }
}