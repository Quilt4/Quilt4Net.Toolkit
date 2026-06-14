using System.Collections.Generic;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Metrics;

/// <summary>
/// Circuit-scoped store for <see cref="MetricsView"/>'s host series (CPU / memory / disk) and the
/// cluster node series (node CPU / memory / filesystem), plus the timestamp they were loaded at.
/// Lets navigating away from /monitor/metrics and back render the charts immediately without
/// re-running the KQL queries, and keeps the refresh-button "Data loaded at …" tooltip honest
/// across navigations. Same lifecycle as <c>LogCountCellCache</c>: scoped (per-circuit), TTL
/// applied on read, force-evicted by the Refresh button. Pod drill-down series are loaded
/// on demand per node and are not cached here.
/// </summary>
internal sealed class MetricsSeriesCache
{
    private readonly Dictionary<(string ConfigKey, System.TimeSpan Range), CacheEntry> _entries = new();

    public bool TryGet(string configKey, System.TimeSpan range, System.TimeSpan maxAge, out CacheEntry entry)
    {
        entry = default;
        if (!_entries.TryGetValue((configKey, range), out var found)) return false;
        if (System.DateTime.UtcNow - found.LoadedUtc > maxAge)
        {
            _entries.Remove((configKey, range));
            return false;
        }
        entry = found;
        return true;
    }

    public void Set(string configKey, System.TimeSpan range,
        MetricSample[] cpu, MetricSample[] memory, MetricSample[] disk, DiskCapacity[] diskCapacity,
        MetricSample[] nodeCpu, MetricSample[] nodeMemory, MetricSample[] nodeFilesystem,
        System.DateTime loadedUtc)
        => _entries[(configKey, range)] = new CacheEntry(cpu, memory, disk, diskCapacity, nodeCpu, nodeMemory, nodeFilesystem, loadedUtc);

    public void Invalidate(string configKey, System.TimeSpan range)
        => _entries.Remove((configKey, range));

    /// <summary>Same key shape as <see cref="LogCountCellCache.KeyForContext"/> — single-context views
    /// derive their key from <see cref="IApplicationInsightsContext.ToKey"/>.</summary>
    public static string KeyForContext(IApplicationInsightsContext context)
        => $"ctx:{context.ToKey()}";

    public readonly record struct CacheEntry(
        MetricSample[] Cpu,
        MetricSample[] Memory,
        MetricSample[] Disk,
        DiskCapacity[] DiskCapacity,
        MetricSample[] NodeCpu,
        MetricSample[] NodeMemory,
        MetricSample[] NodeFilesystem,
        System.DateTime LoadedUtc);
}
