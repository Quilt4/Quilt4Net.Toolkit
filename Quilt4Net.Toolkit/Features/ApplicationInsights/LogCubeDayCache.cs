using System.Collections.Concurrent;
using System.Globalization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Process-wide cache of the log-count cube (<see cref="LogCountByServiceCell"/>), sliced one UTC day
/// at a time. A finished UTC day is immutable — its telemetry never changes — so past-day chunks are
/// cached indefinitely; the current day is refreshed on a short TTL. Multi-day views compose their
/// window from these per-day chunks, so widening the range (or revisiting it after the TTL lapses)
/// re-queries only the live "today" chunk instead of rescanning the whole window in Log Analytics.
///
/// <para>Registered as a singleton so the chunks survive across requests/circuits — that's the point;
/// the scoped <c>LogCountCellCache</c> in the Blazor layer caches the *composed* result per circuit,
/// this caches the *day slices* under it, process-wide.</para>
///
/// <para>Sub-day ranges (1h / 24h) don't use this — a rolling sub-day window can't be assembled from
/// whole-day chunks, so those stay on the live single-query path.</para>
/// </summary>
internal sealed class LogCubeDayCache
{
    private readonly record struct DayEntry(LogCountByServiceCell[] Cells, DateTimeOffset LoadedAt);

    private readonly ConcurrentDictionary<string, DayEntry> _days = new();
    private readonly TimeSpan _todayTtl;
    private readonly Func<DateTimeOffset> _utcNow;

    // Bound memory: a day chunk older than this is dropped on the next access. Comfortably wider than
    // the widest range the UI offers (90 days) so nothing in active use is ever evicted.
    private const int RetentionDays = 100;

    public LogCubeDayCache(TimeSpan todayTtl, Func<DateTimeOffset> utcNow)
    {
        _todayTtl = todayTtl;
        _utcNow = utcNow;
    }

    /// <summary>
    /// Return the cube for a single UTC day, fetching+caching it on a miss. Past days are served from
    /// cache forever once loaded; the current (or, under clock skew, a future) day re-fetches once its
    /// chunk is older than the configured today-TTL. <paramref name="fetch"/> supplies the day from
    /// source — the cache stays agnostic of how the day is queried.
    /// </summary>
    public async Task<LogCountByServiceCell[]> GetDayAsync(string workspaceKey, DateOnly dateUtc, Func<DateOnly, Task<LogCountByServiceCell[]>> fetch)
    {
        var now = _utcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var key = $"{workspaceKey}|{dateUtc:yyyy-MM-dd}";
        var mutable = dateUtc >= today; // today (or a future date from clock skew) can still change

        if (_days.TryGetValue(key, out var entry))
        {
            if (!mutable) return entry.Cells;
            if (now - entry.LoadedAt < _todayTtl) return entry.Cells;
        }

        var cells = await fetch(dateUtc);
        _days[key] = new DayEntry(cells, now);
        Evict(today);
        return cells;
    }

    private void Evict(DateOnly today)
    {
        foreach (var k in _days.Keys)
        {
            var bar = k.LastIndexOf('|');
            if (bar < 0) continue;
            if (DateOnly.TryParse(k.AsSpan(bar + 1), CultureInfo.InvariantCulture, out var d)
                && today.DayNumber - d.DayNumber > RetentionDays)
            {
                _days.TryRemove(k, out _);
            }
        }
    }

    /// <summary>
    /// Sum the cells that share a (Service, Severity, Environment, Source, Machine) key across the
    /// supplied per-day chunks into one cell each — the retained and sampling-corrected figures all
    /// add. Used to fold a composed multi-day window back into the flat cube shape the views expect.
    /// </summary>
    public static LogCountByServiceCell[] Merge(IEnumerable<LogCountByServiceCell> cells)
    {
        var acc = new Dictionary<(string Service, SeverityLevel Severity, string Environment, LogSource Source, string Machine), (long Count, long Bytes, long TrueCount, long TrueBytes)>();
        foreach (var c in cells)
        {
            var key = (c.Service, c.Severity, c.Environment, c.Source, c.Machine);
            var cur = acc.GetValueOrDefault(key);
            acc[key] = (cur.Count + c.Count, cur.Bytes + c.Bytes, cur.TrueCount + c.TrueCount, cur.TrueBytes + c.TrueBytes);
        }
        return acc
            .Select(kv => new LogCountByServiceCell(
                kv.Key.Service, kv.Key.Severity, kv.Key.Environment, kv.Key.Source,
                kv.Value.Count, kv.Value.Bytes, kv.Key.Machine, kv.Value.TrueCount, kv.Value.TrueBytes))
            .ToArray();
    }
}
