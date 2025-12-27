using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public abstract record LogItemBase
{
    public required string Id { get; init; }
    public required string Fingerprint { get; init; }
    public required DateTime TimeGenerated { get; init; }

    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string Application { get; init; }
}

public record LogItem : LogItemBase
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }
}

public record CountData : LogItemBase
{
    public required string Action { get; init; }
    public required int Count { get; init; }
}

public record MeasureData : LogItemBase
{
    public required string Action { get; init; }
    public required TimeSpan Elapsed { get; init; }
}

public record LogDetails : LogItemBase
{
    public required LogSource Source { get; init; }
    public required IReadOnlyDictionary<string, object> Raw { get; init; }
    public required string RawJson { get; init; }
}