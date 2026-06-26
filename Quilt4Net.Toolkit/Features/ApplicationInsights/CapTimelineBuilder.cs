using System;
using System.Collections.Generic;
using System.Linq;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>One hour of billable ingestion volume (MB). Input to <see cref="CapTimelineBuilder"/>.</summary>
public record HourVolume
{
    public required DateTime HourUtc { get; init; }
    public required double Mb { get; init; }
}

/// <summary>
/// Builds a <see cref="CapTimeline"/> from an hourly billable-volume series by detecting gaps (capped
/// periods) in ingestion. A daily cap stops data until the workspace's (variable) reset time, so a cap
/// shows up as a run of hours with no billable volume; the hour data resumes is the reset time. Pure and
/// deterministic — unit-testable without Azure.
/// </summary>
public static class CapTimelineBuilder
{
    /// <param name="hours">Hourly billable volume (MB), ascending and continuous (missing hours filled with 0).</param>
    /// <param name="configuredCapGb">Configured daily cap (GB) from the quota event, if known.</param>
    public static CapTimeline Build(IReadOnlyList<HourVolume> hours, double? configuredCapGb)
    {
        var n = hours?.Count ?? 0;
        var capped = new bool[n];

        if (n == 0)
            return new CapTimeline { CapGb = configuredCapGb, Days = [] };

        // Only count *bounded* no-data runs (data before and after) as caps — that excludes the
        // leading edge before the workspace had any data and the trailing edge (billing lag / an
        // in-progress cap we can't yet measure because data hasn't resumed).
        var firstData = -1;
        var lastData = -1;
        for (var i = 0; i < n; i++)
        {
            if (hours[i].Mb > 0)
            {
                if (firstData < 0) firstData = i;
                lastData = i;
            }
        }

        if (firstData >= 0)
            for (var i = firstData; i <= lastData; i++)
                if (hours[i].Mb <= 0)
                    capped[i] = true;

        // Resume times = the hour data restarts right after a capped run.
        var resumeTimes = new List<DateTime>();
        for (var i = 1; i < n; i++)
            if (capped[i - 1] && !capped[i])
                resumeTimes.Add(hours[i].HourUtc);

        // Detected reset = the most common resume time-of-day across the range.
        TimeSpan? resetUtc = resumeTimes.Count > 0
            ? resumeTimes.GroupBy(t => t.TimeOfDay).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key
            : null;

        // Derived cap size = the largest billable volume across complete reset→next-cap cycles (the quota
        // the cap permits before it stops ingestion).
        double? derivedCapGb = null;
        for (var i = 1; i < n; i++)
        {
            if (!(capped[i - 1] && !capped[i])) continue; // resume at i
            double mb = 0;
            var j = i;
            while (j < n && !capped[j]) { mb += hours[j].Mb; j++; }
            if (j < n && capped[j]) // a complete cycle, ended by a new cap
            {
                var gb = mb / 1024.0;
                derivedCapGb = derivedCapGb.HasValue ? Math.Max(derivedCapGb.Value, gb) : gb;
            }
        }

        // Per-UTC-day aggregation.
        var days = hours
            .Select((h, i) => (h, i))
            .GroupBy(x => x.h.HourUtc.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var idxs = g.Select(x => x.i).ToArray();
                var ingestedGb = idxs.Sum(i => hours[i].Mb) / 1024.0;
                var cappedHours = idxs.Count(i => capped[i]);

                DateTime? resume = null;
                foreach (var i in idxs)
                    if (i > 0 && capped[i - 1] && !capped[i]) { resume = hours[i].HourUtc; break; }

                double? est = cappedHours is > 0 and < 24
                    ? ingestedGb * 24.0 / (24 - cappedHours)
                    : null;

                return new CapDay
                {
                    DateUtc = g.Key,
                    IngestedGb = ingestedGb,
                    GapDuration = cappedHours > 0 ? TimeSpan.FromHours(cappedHours) : null,
                    ResumeUtc = resume,
                    EstimatedUncappedGb = est,
                };
            })
            .ToArray();

        return new CapTimeline
        {
            CapGb = configuredCapGb,
            DerivedCapGb = derivedCapGb,
            CapResetUtc = resetUtc,
            Days = days,
        };
    }
}
