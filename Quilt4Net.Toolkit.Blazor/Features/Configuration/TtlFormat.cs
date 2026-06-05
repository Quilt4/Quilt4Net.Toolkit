namespace Quilt4Net.Toolkit.Blazor.Features.Configuration;

/// <summary>
/// Friendly TimeSpan formatting for TTL/cache-lifetime messages shown to operators in confirmation
/// dialogs and grids. Pads to the largest natural unit so most cases render as a single short term
/// ("5 minutes", "2 hours", "3 days") rather than "0.00:05:00".
/// </summary>
internal static class TtlFormat
{
    public static string Describe(System.TimeSpan? ttl)
    {
        if (!ttl.HasValue) return "the next cache refresh";

        var t = ttl.Value;
        if (t.TotalSeconds < 1) return "less than a second";
        if (t.TotalSeconds < 60) return Plural((int)t.TotalSeconds, "second");
        if (t.TotalMinutes < 60) return Plural((int)t.TotalMinutes, "minute");
        if (t.TotalHours < 24) return Plural((int)t.TotalHours, "hour");
        return Plural((int)t.TotalDays, "day");
    }

    private static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")}";
}
