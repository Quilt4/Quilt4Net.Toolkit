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

    [Fact]
    public void Build_FromCapEvents_DetectsReset_PerDayCappedDuration_AndCapSize()
    {
        // Reset at 12:00 each day; a cap hit at 22:00 day1 (off until 12:00 day2) and 20:00 day2
        // (off until 12:00 day3). So day2 is capped 00:00–12:00 (carry-over) + 20:00–24:00 = 16h.
        var stops = new List<DateTime> { Day1.AddHours(22), Day1.AddDays(1).AddHours(20) };
        var resets = new List<DateTime> { Day1.AddHours(12), Day1.AddDays(1).AddHours(12), Day1.AddDays(2).AddHours(12) };
        var dailyGb = new Dictionary<DateTime, double>
        {
            [Day1.Date] = 8, [Day1.Date.AddDays(1)] = 9, [Day1.Date.AddDays(2)] = 3,
        };

        var timeline = CapTimelineBuilder.Build(stops, resets, dailyGb,
            measuredCapGb: 10, configuredCapGb: null, windowStartUtc: Day1, windowEndUtc: Day1.AddDays(3));

        timeline.CapResetUtc.Should().Be(TimeSpan.FromHours(12));
        timeline.DerivedCapGb.Should().Be(10);

        var day2 = timeline.Days.Single(d => d.DateUtc == Day1.Date.AddDays(1));
        day2.GapDuration.Should().Be(TimeSpan.FromHours(16));            // 12h carry-over + 4h new cap
        day2.ResumeUtc.Should().Be(Day1.AddDays(1).AddHours(12));        // resumed at 12:00

        timeline.Days.Single(d => d.DateUtc == Day1.Date).GapDuration.Should().Be(TimeSpan.FromHours(2));
        timeline.Days.Single(d => d.DateUtc == Day1.Date.AddDays(2)).GapDuration.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public void Build_OpenTrailingCap_ClosesAtWindowEnd()
    {
        // A stop with no following reset is still capped until the window end.
        var stops = new List<DateTime> { Day1.AddHours(20) };
        var resets = new List<DateTime> { Day1.AddHours(12) };
        var dailyGb = new Dictionary<DateTime, double> { [Day1.Date] = 10 };

        var timeline = CapTimelineBuilder.Build(stops, resets, dailyGb,
            measuredCapGb: 10, configuredCapGb: null, windowStartUtc: Day1, windowEndUtc: Day1.AddHours(23));

        timeline.Days.Single(d => d.DateUtc == Day1.Date).GapDuration.Should().Be(TimeSpan.FromHours(3)); // 20:00→23:00
    }

    [Fact]
    public void Build_NoStops_NoCapButResetStillDetected()
    {
        var resets = new List<DateTime> { Day1.AddHours(12), Day1.AddDays(1).AddHours(12) };
        var dailyGb = new Dictionary<DateTime, double> { [Day1.Date] = 4, [Day1.Date.AddDays(1)] = 5 };

        var timeline = CapTimelineBuilder.Build(stops: [], resets: resets, dailyGb: dailyGb,
            measuredCapGb: null, configuredCapGb: null, windowStartUtc: Day1, windowEndUtc: Day1.AddDays(2));

        timeline.CapResetUtc.Should().Be(TimeSpan.FromHours(12));
        timeline.Days.Should().OnlyContain(d => d.GapDuration == null);
        timeline.DerivedCapGb.Should().BeNull();
    }

    [Fact]
    public void Build_Empty_ReturnsEmpty()
    {
        var timeline = CapTimelineBuilder.Build([], [], new Dictionary<DateTime, double>(),
            measuredCapGb: null, configuredCapGb: 5, windowStartUtc: Day1, windowEndUtc: Day1.AddDays(1));

        timeline.Days.Should().BeEmpty();
        timeline.CapResetUtc.Should().BeNull();
        timeline.CapGb.Should().Be(5);
    }
}
