using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummarySubset
{
    public required string Fingerprint { get; init; }

    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string Application { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    public required DateTime LastTimeGenerated { get; init; }
    public required int Count { get; init; }
}