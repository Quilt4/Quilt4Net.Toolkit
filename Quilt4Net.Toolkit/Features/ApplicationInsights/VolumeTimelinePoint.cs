using System;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Billed ingestion volume for a single source table on a single UTC day — the points behind the
/// multi-day logging-volume line chart. Sourced from the workspace <c>Usage</c> table (billing-grade).
/// </summary>
public record VolumeTimelinePoint
{
    /// <summary>The UTC day (start-of-day) the volume was billed on.</summary>
    public required DateTime DayUtc { get; init; }

    /// <summary>Source table / data type (the Log Analytics <c>DataType</c>).</summary>
    public required string Source { get; init; }

    /// <summary>Billed ingestion volume in megabytes for that source on that day.</summary>
    public required double Mb { get; init; }
}
