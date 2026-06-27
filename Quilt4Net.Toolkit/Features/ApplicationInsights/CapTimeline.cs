namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One UTC day of the ingestion-cap timeline: how much was ingested, how long the workspace was capped
/// (no billable data) that day, when data resumed, and an estimate of the uncapped volume.
/// </summary>
public record CapDay
{
    /// <summary>The UTC day (midnight) this entry covers.</summary>
    public required DateTime DateUtc { get; init; }

    /// <summary>Billed volume ingested that day (GB).</summary>
    public required double IngestedGb { get; init; }

    /// <summary>
    /// Total time that day with no billable ingestion attributable to the cap (a bounded no-data gap).
    /// Includes both a carry-over gap at the start of the day and a new cap later the same day.
    /// <c>null</c>/zero when the day wasn't capped.
    /// </summary>
    public TimeSpan? GapDuration { get; init; }

    /// <summary>When billable data first resumed that day after a cap (UTC) — i.e. the observed reset.
    /// <c>null</c> when the day didn't start (or continue) under a cap.</summary>
    public DateTime? ResumeUtc { get; init; }

    /// <summary>
    /// Estimated full-day volume (GB) had the cap not stopped ingestion — extrapolated from the volume
    /// ingested over the uncapped hours of the day. <c>null</c> when the day wasn't capped.
    /// </summary>
    public double? EstimatedUncappedGb { get; init; }

    /// <summary>The capped sub-intervals within this UTC day (clipped to the day) — a day can have a
    /// carry-over gap at the start and a fresh cap later, so there may be more than one.</summary>
    public IReadOnlyList<CappedInterval> CappedIntervals { get; init; } = [];
}

/// <summary>A capped (no-collection) interval, in UTC.</summary>
public record CappedInterval
{
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
}

/// <summary>
/// One cap cycle — the quota period from a reset to the next reset. Unlike a calendar day this aligns
/// to the cap, so a cycle has at most one cap hit and a single clean capped span (hit → next reset).
/// </summary>
public record CapCycle
{
    /// <summary>When the quota reset and collection resumed (UTC) — the start of the cycle.</summary>
    public required DateTime StartUtc { get; init; }

    /// <summary>The next reset (UTC) — the end of the cycle.</summary>
    public required DateTime EndUtc { get; init; }

    /// <summary>Billed volume ingested during the cycle (GB).</summary>
    public required double IngestedGb { get; init; }

    /// <summary>When the cap was hit in this cycle (UTC), or <c>null</c> if it wasn't.</summary>
    public DateTime? CapHitUtc { get; init; }

    /// <summary>How long the cycle was capped (hit → next reset). <c>null</c> when not hit.</summary>
    public TimeSpan? CappedDuration { get; init; }

    /// <summary>Estimated uncapped volume (GB) for the cycle, extrapolated from the uncapped portion.</summary>
    public double? EstimatedUncappedGb { get; init; }
}

/// <summary>
/// Daily ingestion-cap timeline for a workspace, derived from gaps in billable ingestion: the per-day
/// volume + capped duration, the detected daily reset time, and the cap size measured from a full
/// reset-to-cap cycle. Backs the logging dashboard's cap graph.
/// </summary>
public record CapTimeline
{
    /// <summary>Configured daily cap in GB from the workspace "Daily quota changed to N" event, or <c>null</c> if unknown.</summary>
    public double? CapGb { get; init; }

    /// <summary>
    /// Cap size measured from observed data — the billable volume in a complete reset→cap cycle (GB).
    /// <c>null</c> when no complete capped cycle was observed in range. Prefer this over <see cref="CapGb"/>
    /// for display when available, since it reflects what was actually ingested before the cap hit.
    /// </summary>
    public double? DerivedCapGb { get; init; }

    /// <summary>
    /// The detected daily cap reset time-of-day (UTC) — when ingestion resumes after a cap, taken from the
    /// most common resume time across the range. <c>null</c> when no cap/reset was observed.
    /// </summary>
    public TimeSpan? CapResetUtc { get; init; }

    /// <summary>Per-calendar-UTC-day entries, newest first.</summary>
    public required IReadOnlyList<CapDay> Days { get; init; }

    /// <summary>Per-cap-cycle entries (reset → next reset), newest first. Empty when no resets were observed.</summary>
    public IReadOnlyList<CapCycle> Cycles { get; init; } = [];
}
