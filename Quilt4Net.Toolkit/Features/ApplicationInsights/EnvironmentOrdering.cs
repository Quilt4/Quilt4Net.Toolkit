namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Helpers for ordering environment names consistently across the version matrix UI.
/// </summary>
public static class EnvironmentOrdering
{
    /// <summary>The default order applied when no <c>EnvironmentOrder</c> is configured.</summary>
    public static readonly IReadOnlyList<string> DefaultOrder = ["Development", "CI", "Staging", "Test", "Production"];

    internal const string Unknown = "(unknown)";

    /// <summary>
    /// Order <paramref name="environments"/> by the supplied <paramref name="preferredOrder"/>:
    /// listed names first (in the supplied order), then unlisted names alphabetically, then
    /// <c>(unknown)</c> last. When <paramref name="preferredOrder"/> is null/empty,
    /// <see cref="DefaultOrder"/> is used.
    /// </summary>
    public static IReadOnlyList<string> Order(IEnumerable<string> environments, IReadOnlyList<string> preferredOrder = null)
    {
        var order = preferredOrder is { Count: > 0 } ? preferredOrder : DefaultOrder;

        var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < order.Count; i++)
        {
            ranks.TryAdd(order[i], i);
        }

        return environments
            .Select(e => string.IsNullOrWhiteSpace(e) ? Unknown : e)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => string.Equals(e, Unknown, StringComparison.OrdinalIgnoreCase) ? int.MaxValue : ranks.GetValueOrDefault(e, ranks.Count + 1))
            .ThenBy(e => e, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
