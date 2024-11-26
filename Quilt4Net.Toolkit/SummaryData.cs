using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit;

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

public record SummaryDataIdentifier
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogType Type { get; init; }
    public required string Identifier { get; init; }
    public required string Application { get; init; }
}

public enum LogType
{
    Exception,
    Trace
}