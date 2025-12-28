namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record CountData : LogItemBase
{
    public required string Action { get; init; }
    public required int Count { get; init; }
}