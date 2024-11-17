using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

public record ReadyResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReadyStatusResult Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, Component> Components { get; init; }
}