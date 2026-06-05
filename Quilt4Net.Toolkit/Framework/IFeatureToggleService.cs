namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Minimal feature-toggle accessor: resolves a boolean flag from Quilt4Net remote configuration.
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>
    /// Resolves a boolean feature toggle. The value is cached locally with stale-while-revalidate and
    /// falls back to <paramref name="fallback"/> when the key is unknown or the server is unreachable.
    /// </summary>
    /// <param name="key">Toggle key.</param>
    /// <param name="fallback">Value returned when the key is unknown or the server is unreachable.</param>
    /// <param name="ttl">
    /// Optional client-requested cache lifetime. When <c>null</c>, the team/server-configured default applies.
    /// </param>
    /// <param name="application">
    /// Which application's toggle to read: <b>empty string (the default)</b> reads the <b>shared</b>,
    /// cross-application value; <c>null</c> reads the configured application's value (or the entry assembly
    /// name); a specific name reads that named application's value.
    /// </param>
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null, string application = "");
}
