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
        DateTime windowEndUtc)
    {
        stops ??= [];
        resets ??= [];
        dailyGb ??= new Dictionary<DateTime, double>();

        var sortedResets = resets.OrderBy(r => r).ToList();

        // Capped intervals: collection is off from each stop until the next reset (or the window end).
        var intervals = new List<(DateTime Start, DateTime End)>();
        foreach (var stop in stops.OrderBy(s => s))
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

            var cappedSpan = intervals.Aggregate(TimeSpan.Zero, (acc, iv) =>
            {
                var os = iv.Start > dayStart ? iv.Start : dayStart;
                var oe = iv.End < dayEnd ? iv.End : dayEnd;
                return oe > os ? acc + (oe - os) : acc;
            });

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
            };
        }).ToArray();

        return new CapTimeline
        {
            CapGb = configuredCapGb,
            DerivedCapGb = measuredCapGb,
            CapResetUtc = resetUtc,
            Days = capDays,
        };
    }
}
