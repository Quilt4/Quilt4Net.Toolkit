using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummaryData
{
    public required string SummaryId { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Message { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    public required DateTime? LastSeen { get; init; }
    public required int IssueCount { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Type { get; init; }
}

public record MeasureData
{
    public required DateTimeOffset TimeGenerated { get; init; }
    public required string Action { get; init;  }
    public required string ApplicationName { get; init;  }
    public required string Environment { get; init; }
    public required TimeSpan Elapsed { get; init; }
}