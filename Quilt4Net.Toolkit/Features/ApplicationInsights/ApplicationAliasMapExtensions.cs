namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public static class ApplicationAliasMapExtensions
{
    /// <summary>
    /// Flatten an <see cref="ApplicationAliasMap"/> list into a case-insensitive
    /// <c>raw → logical</c> dictionary suitable for per-row alias resolution in log
    /// views (where rows are not folded together — each row's raw application name
    /// just needs to be displayed under its logical name).
    /// </summary>
    public static IReadOnlyDictionary<string, string> ToResolverDictionary(this IReadOnlyList<ApplicationAliasMap> aliases)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (aliases is null) return dict;
        foreach (var alias in aliases)
        {
            if (string.IsNullOrEmpty(alias?.LogicalName)) continue;
            foreach (var src in alias.SourceNames ?? [])
            {
                if (!string.IsNullOrWhiteSpace(src)) dict[src] = alias.LogicalName;
            }
        }
        return dict;
    }
}
