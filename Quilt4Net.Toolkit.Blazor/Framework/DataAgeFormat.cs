namespace Quilt4Net.Toolkit.Blazor.Framework;

/// <summary>
/// Friendly "how old is this data" string for tooltips on refresh buttons. The exact value matters
/// less than the order-of-magnitude — operators glance at it to decide whether to hit refresh.
/// Recomputed on every render: re-renders are frequent enough that the displayed age stays close
/// to truth without needing a separate ticker timer.
/// </summary>
public static class DataAgeFormat
{
    public static string Describe(System.DateTime? lastRefreshUtc)
    {
        if (!lastRefreshUtc.HasValue) return "No data loaded yet.";

        var age = System.DateTime.UtcNow - lastRefreshUtc.Value;
        if (age < System.TimeSpan.Zero) age = System.TimeSpan.Zero;

        if (age.TotalSeconds < 10) return "Data loaded just now.";
        if (age.TotalSeconds < 60) return $"Data loaded {(int)age.TotalSeconds} seconds ago.";
        if (age.TotalMinutes < 60) return $"Data loaded {Plural((int)age.TotalMinutes, "minute")} ago.";
        if (age.TotalHours < 24)   return $"Data loaded {Plural((int)age.TotalHours, "hour")} ago.";
        return $"Data loaded {Plural((int)age.TotalDays, "day")} ago.";
    }

    private static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")}";
}
