namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One day of the ingestion-cap timeline: how much was ingested, whether/when the daily cap was hit,
/// and an estimate of what the day's volume would have been had the cap not stopped ingestion.
/// </summary>
public record CapDay
{
    /// <summary>The UTC day (midnight) this entry covers.</summary>
    public required DateTime DateUtc { get; init; }

    /// <summary>Billed volume ingested that day (GB). On a capped day this is roughly the cap.</summary>
    public required double IngestedGb { get; init; }

    /// <summary>When the daily cap was hit (UTC), or <c>null</c> if it wasn't hit that day.</summary>
    public DateTime? CapHitUtc { get; init; }

    /// <summary>
    /// Estimated full-day volume (GB) had the cap not stopped ingestion — extrapolated from the
    /// pre-cap rate (cap ÷ fraction-of-day-elapsed-at-hit). <c>null</c> when the cap wasn't hit, in
    /// which case <see cref="IngestedGb"/> is already the real full-day volume.
    /// </summary>
    public double? EstimatedUncappedGb { get; init; }
}

/// <summary>
/// Daily ingestion-cap timeline for a workspace: the configured daily cap plus a per-day breakdown of
/// volume and cap-hit time. Backs the logging dashboard's cap graph.
/// </summary>
public record CapTimeline
{
    /// <summary>Configured daily cap in GB, or <c>null</c> if it couldn't be determined.</summary>
    public double? CapGb { get; init; }

    /// <summary>Per-day entries, newest first.</summary>
    public required IReadOnlyList<CapDay> Days { get; init; }
}
