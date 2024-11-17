using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

public record Component
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReadyStatusResult Status { get; init; }
}