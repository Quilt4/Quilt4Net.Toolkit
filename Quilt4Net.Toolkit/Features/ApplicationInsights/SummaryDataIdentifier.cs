using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummaryDataIdentifier
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogType Type { get; init; }
    //public string Id { get; init; }
    public required string Identifier { get; init; }
    public required string Application { get; init; }
}