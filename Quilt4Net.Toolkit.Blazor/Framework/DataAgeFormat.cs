namespace Quilt4Net.Toolkit.Blazor.Framework;

/// <summary>
/// Tooltip text for refresh buttons — "when was this data last loaded" plus an optional cache-TTL
/// hint. Reports the absolute timestamp (in the supplied timezone, defaulting to
/// <see cref="System.TimeZoneInfo.Local"/>) rather than a relative "X minutes ago" age. Absolute
/// time is easier to reason about across page navigations: the timestamp is whatever was stamped at
/// fetch time, even if the user has since flipped between pages, and the displayed string doesn't
/// drift the way an age string does between renders. Components hand this to the native HTML
/// <c>title</c> attribute — no JS round-trip, fixed text per render, recomputed on the next render
/// after a fresh fetch / cache restore.
/// </summary>
public static class DataAgeFormat
{
    /// <summary>
    /// Format the timestamp in <paramref name="zone"/> (defaults to <see cref="System.TimeZoneInfo.Local"/>).
    /// Components that inject <c>IBrowserTimeZoneAccessor</c> should pass its <c>Current</c> so the
    /// rendered time matches what the operator sees on their own clock. When <paramref name="cacheTtl"/>
    /// is supplied a "Cached for X" sentence is appended — purely informational so the operator
    /// knows roughly when the next automatic re-fetch would kick in if they don't press Refresh.
    /// </summary>
    public static string Describe(System.DateTime? lastRefreshUtc, System.TimeZoneInfo zone = null, System.TimeSpan? cacheTtl = null)
    {
        var primary = DescribeLoad(lastRefreshUtc, zone);
        return cacheTtl.HasValue
            ? $"{primary} Cached for {FormatTtl(cacheTtl.Value)}."
            : primary;
    }

    private static string DescribeLoad(System.DateTime? lastRefreshUtc, System.TimeZoneInfo zone)
    {
        if (!lastRefreshUtc.HasValue) return "No data loaded yet.";

        zone ??= System.TimeZoneInfo.Local;
        // Defensive: if the caller hands us a DateTime that already declares Local/Unspecified
        // kind, .ToUniversalTime() normalises it before the zone conversion.
        var utc = lastRefreshUtc.Value.Kind == System.DateTimeKind.Utc
            ? lastRefreshUtc.Value
            : lastRefreshUtc.Value.ToUniversalTime();
        var local = System.TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        var today = System.TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, zone).Date;
        // Same-day loads are the common case — drop the date to keep the tooltip short. Older loads
        // (page left open overnight, long-lived 1-hour cache on Version) keep the full date so the
        // operator can tell at a glance that the data is from yesterday.
        return local.Date == today
            ? $"Data loaded at {local:HH:mm:ss}."
            : $"Data loaded at {local:yyyy-MM-dd HH:mm:ss}.";
    }

    /// <summary>Round to the nearest whole hour / minute / second so the tooltip reads naturally.</summary>
    private static string FormatTtl(System.TimeSpan ttl)
    {
        if (ttl.TotalHours >= 1)
        {
            var hours = (int)System.Math.Round(ttl.TotalHours);
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }
        if (ttl.TotalMinutes >= 1)
        {
            var minutes = (int)System.Math.Round(ttl.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
        var seconds = (int)System.Math.Round(ttl.TotalSeconds);
        return seconds == 1 ? "1 second" : $"{seconds} seconds";
    }
}
