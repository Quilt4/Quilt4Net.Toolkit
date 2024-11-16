using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Live;

public record LiveResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LiveStatusResult Status { get; init; }
}