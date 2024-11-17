using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Health;

public record HealthResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HealthStatusResult Status { get; init; }

    public required Dictionary<string, Component> Components { get; init; }
}