namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogDetails : LogItemBase
{
    public required LogSource Source { get; init; }
    public required SeverityLevel SeverityLevel { get; init; }
    public required string CorrelationId { get; init; }
    public required IReadOnlyDictionary<string, object> Raw { get; init; }
    public required string RawJson { get; init; }
}