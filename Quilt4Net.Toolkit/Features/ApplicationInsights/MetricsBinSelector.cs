namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Picks a histogram bin size that yields ~60–300 points per host across the requested time
/// window. Keeps charts readable and KQL responses bounded — without an adaptive bin a 7-day
/// window at 1-minute granularity returns ~10k points per host.
/// </summary>
internal static class MetricsBinSelector
{
    public static System.TimeSpan PickBin(System.TimeSpan window)
    {
        if (window <= System.TimeSpan.FromHours(1))    return System.TimeSpan.FromMinutes(1);
        if (window <= System.TimeSpan.FromHours(6))    return System.TimeSpan.FromMinutes(5);
        if (window <= System.TimeSpan.FromHours(24))   return System.TimeSpan.FromMinutes(10);
        if (window <= System.TimeSpan.FromDays(3))     return System.TimeSpan.FromMinutes(30);
        return System.TimeSpan.FromHours(1);
    }

    /// <summary>KQL <c>timespan</c> literal — e.g. <c>1m</c>, <c>10m</c>, <c>1h</c>.</summary>
    public static string ToKqlLiteral(System.TimeSpan bin)
    {
        if (bin.TotalDays   >= 1 && bin == System.TimeSpan.FromDays((int)bin.TotalDays))     return $"{(int)bin.TotalDays}d";
        if (bin.TotalHours  >= 1 && bin == System.TimeSpan.FromHours((int)bin.TotalHours))   return $"{(int)bin.TotalHours}h";
        if (bin.TotalMinutes >= 1 && bin == System.TimeSpan.FromMinutes((int)bin.TotalMinutes)) return $"{(int)bin.TotalMinutes}m";
        return $"{(int)bin.TotalSeconds}s";
    }
}
