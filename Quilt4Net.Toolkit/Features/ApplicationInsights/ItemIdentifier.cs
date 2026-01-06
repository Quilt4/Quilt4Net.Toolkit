using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record ItemIdentifier
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Type { get; init; }
    public required string Identifier { get; init; }
}