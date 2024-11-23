namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Metrics.
/// </summary>
public record MetricsResponse
{
    internal MetricsResponse()
    {
    }

    /// <summary>
    /// The time that the application has been running.
    /// </summary>
    public required TimeSpan ApplicationUptime { get; init; }

    /// <summary>
    /// Memory information.
    /// </summary>
    public required Memory Memory { get; init; }

    /// <summary>
    /// Processor information.
    /// </summary>
    public required Processor Processor { get; init; }
}