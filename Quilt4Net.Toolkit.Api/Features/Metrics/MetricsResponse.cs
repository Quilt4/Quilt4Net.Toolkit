namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public record MetricsResponse
{
    public required TimeSpan ApplicationUptime { get; init; }
    public required Memory Memory { get; init; }
    public required Processor Processor { get; init; }
}