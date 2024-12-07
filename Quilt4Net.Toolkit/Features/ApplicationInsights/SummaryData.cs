using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummaryData
{
    public required string SummaryIdentifier { get; init; }
    public required string Application { get; init; }
    public required LogType Type { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }
    public required int IssueCount { get; init; }
    public required string Message { get; init; }
}