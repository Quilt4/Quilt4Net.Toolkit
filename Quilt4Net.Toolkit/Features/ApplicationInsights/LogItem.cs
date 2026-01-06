using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogItem : LogItemBase
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }
}