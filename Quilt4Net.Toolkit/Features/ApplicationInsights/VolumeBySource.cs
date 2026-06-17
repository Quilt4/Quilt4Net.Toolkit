namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Billed ingestion volume for a single source table (Log Analytics <c>DataType</c>, e.g.
/// <c>AppMetrics</c>, <c>AppTraces</c>) over a window — the cost breakdown that drives the logging
/// cost dashboard's source pie. Sourced from the workspace <c>Usage</c> table (billing-grade).
/// </summary>
public record VolumeBySource
{
    /// <summary>Source table / data type (the Log Analytics <c>DataType</c>).</summary>
    public required string Source { get; init; }

    /// <summary>Billed ingestion volume in megabytes over the window.</summary>
    public required double Mb { get; init; }
}
