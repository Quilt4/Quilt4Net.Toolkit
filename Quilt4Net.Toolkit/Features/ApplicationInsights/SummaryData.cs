using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummaryData
{
    public required string Fingerprint { get; init; }

    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string Application { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    public required Item[] Items { get; init; }

    public record Item
    {
        public required string Id { get; init; }
        public required DateTime TimeGenerated { get; init; }
        public required string Message { get; init; }
    }
}