using FluentAssertions;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

/// <summary>
/// The summary list KQL used to bucket by (Fingerprint, Message, Environment, Application, SeverityLevel),
/// which on busy workspaces produced a row per (fingerprint × message variant) and could blow past
/// Kusto's 64MB result-set limit. This file pins the new shape so a future edit can't regress to the
/// over-keyed grouping.
/// </summary>
public class SummarizeByFingerprintTests
{
    [Fact]
    public void Buckets_by_fingerprint_only()
    {
        ApplicationInsightsService.SummarizeByFingerprint
            .Should().Contain("by Fingerprint")
            .And.NotContain("by Fingerprint, Message")
            .And.NotContain("by Fingerprint, Environment")
            .And.NotContain("by Fingerprint, Application");
    }

    [Fact]
    public void Takes_a_representative_sample_for_descriptive_columns()
    {
        ApplicationInsightsService.SummarizeByFingerprint
            .Should().Contain("Message = take_any(Message)")
            .And.Contain("Environment = take_any(Environment)")
            .And.Contain("Application = take_any(Application)");
    }

    [Fact]
    public void Aggregates_severity_to_the_max_observed()
    {
        ApplicationInsightsService.SummarizeByFingerprint
            .Should().Contain("SeverityLevel = max(SeverityLevel)");
    }

    [Fact]
    public void Counts_occurrences_and_tracks_last_seen()
    {
        ApplicationInsightsService.SummarizeByFingerprint
            .Should().Contain("Count = count()")
            .And.Contain("LastTimeGenerated = max(TimeGenerated)");
    }

    [Fact]
    public void Orders_results_so_the_C_sharp_layer_dedupe_keeps_the_most_recent()
    {
        ApplicationInsightsService.SummarizeByFingerprint
            .Should().Contain("order by LastTimeGenerated desc");
    }
}
