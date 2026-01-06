namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record MeasureData : LogItemBase
{
    public required string Action { get; init; }
    public required TimeSpan Elapsed { get; init; }
}