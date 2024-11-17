using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Health;

public record Component
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HealthStatusResult Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, string> Details { get; init; }
}