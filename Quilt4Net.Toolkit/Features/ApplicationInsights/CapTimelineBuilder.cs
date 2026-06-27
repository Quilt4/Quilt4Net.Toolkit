using System;
using System.Collections.Generic;
using System.Linq;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Builds a <see cref="CapTimeline"/> from the workspace's authoritative daily-cap Operation events:
/// "data collection stopped due to daily limit" (cap hit) and "data collection started … daily limit
/// reset" (reset/resume). Collection is off between a stop and the next reset; per-UTC-day capped time
/// is the overlap of those intervals with the day, and the reset time is taken from the reset events
/// (precise, unlike the ~1h-lagged Usage table). Pure and deterministic — unit-testable without Azure.
/// </summary>
public static class CapTimelineBuilder
{
    /// <param name="stops">Cap-hit event times (UTC).</param>
    /// <param name="resets">Daily-reset event times (UTC) — when collection resumed.</param>
    /// <param name="dailyGb">Billed volume per UTC day (GB).</param>
    /// <param name="measuredCapGb">Cap size measured from a full reset cycle (GB), already rounded; or null.</param>
    /// <param name="configuredCapGb">Configured daily cap (GB) from the quota event, if known.</param>
    /// <param name="windowStartUtc">Start of the analysed window (used to close an open trailing cap).</param>
    /// <param name="windowEndUtc">End of the analysed window (used to close an open trailing cap).</param>
    public static CapTimeline Build(
        IReadOnlyList<DateTime> stops,
        IReadOnlyList<DateTime> resets,
        IReadOnlyDictionary<DateTime, double> dailyGb,
        double? measuredCapGb,
        double? configuredCapGb,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        IReadOnlyDictionary<DateTime, double> cycleGbByStart = null)
    {
        stops ??= [];
        resets ??= [];
        dailyGb ??= new Dictionary<DateTime, double>();

        var sortedStops = stops.OrderBy(s => s).ToList();
        var sortedResets = resets.OrderBy(r => r).ToList();

        // Capped intervals: collection is off from each stop until the next reset (or the window end).
        var intervals = new List<(DateTime Start, DateTime End)>();
        foreach (var stop in sortedStops)
        {
            var nextReset = sortedResets.FirstOrDefault(r => r > stop);
            var end = nextReset != default ? nextReset : windowEndUtc;
            if (end > stop) intervals.Add((stop, end));
        }

        // Reset time-of-day = the most common reset-event hour.
        TimeSpan? resetUtc = resets.Count > 0
            ? resets.GroupBy(r => TimeSpan.FromHours(Math.Round(r.TimeOfDay.TotalHours)))
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key
            : null;

        // Days to render: union of days with volume and days touched by a capped interval.
        var days = new HashSet<DateTime>(dailyGb.Keys.Select(d => d.Date));
        foreach (var (s, e) in intervals)
            for (var d = s.Date; d <= e.Date; d = d.AddDays(1))
                days.Add(d);

        var capDays = days.OrderByDescending(d => d).Select(day =>
        {
            var dayStart = day;
            var dayEnd = day.AddDays(1);

            // Clip each capped interval to the day; a day can hold a carry-over gap and a fresh cap.
            var clipped = new List<CappedInterval>();
            foreach (var iv in intervals)
            {
                var os = iv.Start > dayStart ? iv.Start : dayStart;
                var oe = iv.End < dayEnd ? iv.End : dayEnd;
                if (oe > os) clipped.Add(new CappedInterval { StartUtc = os, EndUtc = oe });
            }
            var cappedSpan = clipped.Aggregate(TimeSpan.Zero, (acc, c) => acc + (c.EndUtc - c.StartUtc));

            var ingested = dailyGb.GetValueOrDefault(day);
            DateTime? resume = sortedResets.Where(r => r >= dayStart && r < dayEnd).Select(r => (DateTime?)r).FirstOrDefault();

            double? est = null;
            var cappedHours = cappedSpan.TotalHours;
            if (cappedHours > 0 && cappedHours < 24)
                est = ingested * 24.0 / (24 - cappedHours);

            return new CapDay
            {
                DateUtc = day,
                IngestedGb = ingested,
                GapDuration = cappedSpan > TimeSpan.Zero ? cappedSpan : null,
                ResumeUtc = resume,
                EstimatedUncappedGb = est,
                CappedIntervals = clipped,
            };
        }).ToArray();

        return new CapTimeline
        {
            CapGb = configuredCapGb,
            DerivedCapGb = measuredCapGb,
            CapResetUtc = resetUtc,
            Days = capDays,
            Cycles = BuildCycles(sortedStops, sortedResets, cycleGbByStart, windowEndUtc),
        };
    }

    // One row per quota cycle (reset → next reset) for *every* day with data — not just capped days.
    // The "daily limit reset" event only fires after a cap, so we anchor cycles to every cycle-start
    // that has billable volume (plus any reset boundary), giving a continuous timeline. A cycle has at
    // most one cap hit and a single clean capped span (hit → next reset).
    private static IReadOnlyList<CapCycle> BuildCycles(
        List<DateTime> sortedStops,
        List<DateTime> sortedResets,
        IReadOnlyDictionary<DateTime, double> cycleGbByStart,
        DateTime windowEndUtc)
    {
        // Per-cycle volume keys are already one-per-day at the reset hour; prefer them. Fall back to reset
        // events only when there's no volume data. Dedupe to one start per calendar day — reset events can
        // fire at slightly different hours (e.g. 11:00 vs 12:00), which would otherwise create two cycles
        // for the same day and collide as duplicate chart categories.
        IEnumerable<DateTime> candidates = cycleGbByStart is { Count: > 0 }
            ? cycleGbByStart.Keys
            : sortedResets;
        var startList = candidates
            .GroupBy(s => s.Date)
            .Select(g => g.Min())
            .OrderBy(s => s)
            .ToList();
        if (startList.Count == 0) return [];
        var cycles = new List<CapCycle>();
        for (var i = 0; i < startList.Count; i++)
        {
            var start = startList[i];
            // Cycle ends at the next cycle start, or one day on (clamped to the window) for the last.
            var end = i + 1 < startList.Count
                ? startList[i + 1]
                : (start.AddDays(1) < windowEndUtc ? start.AddDays(1) : windowEndUtc);
            if (end <= start) end = start.AddDays(1);

            var hit = sortedStops.Where(s => s >= start && s < end).Select(s => (DateTime?)s).FirstOrDefault();
            var ingested = LookupCycleGb(cycleGbByStart, start);

            TimeSpan? cappedDuration = hit.HasValue ? end - hit.Value : null;
            double? est = null;
            if (hit.HasValue)
            {
                var cycleHours = (end - start).TotalHours;
                var cappedHours = cappedDuration.Value.TotalHours;
                if (cycleHours > 0 && cappedHours < cycleHours)
                    est = ingested * cycleHours / (cycleHours - cappedHours);
            }

            cycles.Add(new CapCycle
            {
                StartUtc = start,
                EndUtc = end,
                IngestedGb = ingested,
                CapHitUtc = hit,
                CappedDuration = cappedDuration,
                EstimatedUncappedGb = est,
            });
        }
        return cycles.OrderByDescending(c => c.StartUtc).ToArray();
    }

    private static double LookupCycleGb(IReadOnlyDictionary<DateTime, double> cycleGbByStart, DateTime start)
    {
        if (cycleGbByStart == null || cycleGbByStart.Count == 0) return 0;
        if (cycleGbByStart.TryGetValue(start, out var exact)) return exact;
        // Nearest cycle-start within 6h (the binned start may differ from the reset event by minutes/lag).
        var best = cycleGbByStart
            .Where(kvp => Math.Abs((kvp.Key - start).TotalHours) <= 6)
            .OrderBy(kvp => Math.Abs((kvp.Key - start).TotalHours))
            .Select(kvp => (double?)kvp.Value)
            .FirstOrDefault();
        return best ?? 0;
    }
}
