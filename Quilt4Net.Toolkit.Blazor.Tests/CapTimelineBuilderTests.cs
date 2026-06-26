using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class CapTimelineBuilderTests
{
    private static readonly DateTime Day1 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Three UTC days at 1 MB/hour, with two caps that each run 23:00 → 05:00 the next day
    // (reset at 05:00). The middle day is thus capped 00:00–05:00 (carry-over) AND 23:00–24:00 = 6h.
    private static List<HourVolume> BuildScenario()
    {
        var hours = new List<HourVolume>();
        for (var i = 0; i < 72; i++)
            hours.Add(new HourVolume { HourUtc = Day1.AddHours(i), Mb = 1.0 });

        void Cap(DateTime from, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var at = from.AddHours(i);
                var idx = hours.FindIndex(h => h.HourUtc == at);
                hours[idx] = hours[idx] with { Mb = 0 };
            }
        }

        Cap(Day1.AddHours(23), 6);           // 23:00 day1 → 04:59 day2  (resume 05:00 day2)
        Cap(Day1.AddDays(1).AddHours(23), 6); // 23:00 day2 → 04:59 day3  (resume 05:00 day3)
        return hours;
    }

    [Fact]
    public void Build_DetectsResetTime_PerDayGap_AndDerivedCapSize()
    {
        var timeline = CapTimelineBuilder.Build(BuildScenario(), configuredCapGb: null);

        // Reset detected from where data resumes after a cap.
        timeline.CapResetUtc.Should().Be(TimeSpan.FromHours(5));

        var day2 = timeline.Days.Single(d => d.DateUtc == Day1.Date.AddDays(1));
        day2.GapDuration.Should().Be(TimeSpan.FromHours(6));               // 5h carry-over + 1h new cap
        day2.ResumeUtc.Should().Be(Day1.AddDays(1).AddHours(5));           // resumed at 05:00

        timeline.Days.Single(d => d.DateUtc == Day1.Date).GapDuration.Should().Be(TimeSpan.FromHours(1));
        timeline.Days.Single(d => d.DateUtc == Day1.Date.AddDays(2)).GapDuration.Should().Be(TimeSpan.FromHours(5));

        // Cap size = volume in a complete reset→next-cap cycle: 05:00→23:00 = 18h × 1 MB = 18 MB.
        timeline.DerivedCapGb.Should().BeApproximately(18.0 / 1024.0, 1e-9);
    }

    [Fact]
    public void Build_NoData_ReturnsEmpty()
    {
        var timeline = CapTimelineBuilder.Build(new List<HourVolume>(), configuredCapGb: 5);
        timeline.Days.Should().BeEmpty();
        timeline.CapResetUtc.Should().BeNull();
        timeline.CapGb.Should().Be(5);
    }

    [Fact]
    public void Build_NoGaps_NoCapReported()
    {
        var hours = Enumerable.Range(0, 48).Select(i => new HourVolume { HourUtc = Day1.AddHours(i), Mb = 2.0 }).ToList();
        var timeline = CapTimelineBuilder.Build(hours, configuredCapGb: null);

        timeline.Days.Should().OnlyContain(d => d.GapDuration == null);
        timeline.CapResetUtc.Should().BeNull();
        timeline.DerivedCapGb.Should().BeNull();
    }
}
