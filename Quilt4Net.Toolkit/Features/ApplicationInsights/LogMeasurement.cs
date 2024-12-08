namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogMeasurement
{
    public required string Application { get; init; }
    public required DateTime TimeGenerated { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required string CategoryName { get; init; }
    public required string Method { get; init; }
    public required string Action { get; init; }
}