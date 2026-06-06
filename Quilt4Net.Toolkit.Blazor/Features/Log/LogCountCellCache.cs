using System.Collections.Generic;
using System.Linq;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

/// <summary>
/// Circuit-scoped store for <see cref="LogCountByServiceView"/>'s per-range cell cube. The cube is
/// expensive to fetch (one Log Analytics query per workspace) and the in-component dictionary that
/// previously held it died with the component on every page navigation; lifting it into a scoped
/// service lets the cube survive nav within the same Blazor circuit so flipping away from
/// /monitor/logcount and back doesn't trigger a re-fetch (or a spinner). Cache entries carry the
/// timestamp they were loaded at, so the tooltip on the refresh button reports the real fetch time
/// even after navigating away and back — without that timestamp, the component would mistakenly
/// announce "Data loaded just now" every time it re-mounted on cached data.
/// </summary>
internal sealed class LogCountCellCache
{
    private readonly Dictionary<(string ConfigsKey, System.TimeSpan Range), CacheEntry> _entries = new();

    public bool TryGet(string configsKey, System.TimeSpan range, System.TimeSpan maxAge, out CacheEntry entry)
    {
        entry = default;
        if (!_entries.TryGetValue((configsKey, range), out var found)) return false;
        // Age-out stale entries on read instead of running a background sweeper. The first access
        // after the TTL lapses sees the miss and triggers a re-fetch; nothing else is required.
        if (System.DateTime.UtcNow - found.LoadedUtc > maxAge)
        {
            _entries.Remove((configsKey, range));
            return false;
        }
        entry = found;
        return true;
    }

    public void Set(string configsKey, System.TimeSpan range, IReadOnlyList<LogCountTaggedCell> cells, System.DateTime loadedUtc)
        => _entries[(configsKey, range)] = new CacheEntry(cells, loadedUtc);

    public void Invalidate(string configsKey, System.TimeSpan range)
        => _entries.Remove((configsKey, range));

    /// <summary>
    /// Build a stable key for a set of configurations: the ids joined by '|' in case-insensitive order
    /// so the order in which the user enumerates them doesn't fragment the cache. An explicit
    /// <c>IApplicationInsightsContext</c> (rather than a list of configurations) uses its
    /// <see cref="IApplicationInsightsContext.ToKey()"/>.
    /// </summary>
    public static string KeyForConfigs(IEnumerable<string> configIds)
        => string.Join("|", (configIds ?? []).OrderBy(x => x, System.StringComparer.OrdinalIgnoreCase));

    public static string KeyForContext(IApplicationInsightsContext context)
        => $"ctx:{context.ToKey()}";

    /// <summary>
    /// Cached cube plus the moment it was loaded — passed back to <see cref="LogCountByServiceView"/>
    /// so its refresh-button tooltip reports the real fetch time, not a freshly-stamped "now".
    /// </summary>
    public readonly record struct CacheEntry(IReadOnlyList<LogCountTaggedCell> Cells, System.DateTime LoadedUtc);
}

/// <summary>
/// A single cell paired with the configuration id it came from, so the configuration multi-select
/// can be applied as a local re-group over the cube without another KQL round-trip.
/// </summary>
internal sealed record LogCountTaggedCell(string ConfigId, LogCountByServiceCell Cell);
