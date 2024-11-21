using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit;

public record SummaryData
{
    public string AppRoleName { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SeverityLevel SeverityLevel { get; init; }
    public string ProblemId { get; init; }
    public int IssueCount { get; init; }
}