using System;
using System.Linq;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Metrics;

/// <summary>
/// Pure helpers for <see cref="MetricsView"/> — extracted so the node→pod drill-down logic can be
/// unit-tested without rendering the Radzen charts (which need a real browser for layout).
/// </summary>
internal static class MetricsViewLogic
{
    /// <summary>
    /// Distinct, case-insensitively sorted node names from a cluster-node metric series, used to
    /// populate the drill-down dropdown. Empty/whitespace series are dropped (a sample with no
    /// <c>k8s.node.name</c> shouldn't appear as a selectable node).
    /// </summary>
    public static string[] NodeNames(MetricSample[] nodeSamples)
        => (nodeSamples ?? [])
            .Select(s => s.Series ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// Distinct node names across several node series (union). Used so the cluster tab / drill-down
    /// list reflects every node seen in any chart — important because the CPU% series only has rows
    /// where <c>allocatable_cpu</c> overlaps the window, while memory/filesystem are always present.
    /// </summary>
    public static string[] NodeNames(params MetricSample[][] sets)
        => (sets ?? [])
            .Where(s => s != null)
            .SelectMany(s => s)
            .Select(s => s.Series ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
