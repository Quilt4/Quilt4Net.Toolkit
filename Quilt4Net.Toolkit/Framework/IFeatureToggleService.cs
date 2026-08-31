using Quilt4Net.Toolkit.Features.FeatureToggle;

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

    /// <summary>
    /// As <see cref="GetToggleAsync"/>, but also reports where the value came from
    /// (<see cref="ConfigurationSource"/>) — server, cache, stale cache or the caller's fallback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetToggleAsync"/> cannot express the difference between "the server says this
    /// feature is off" and "nothing has answered, so you are holding your own fallback". Under a
    /// sustained fault those are the same <c>false</c>, which is how a toggle stayed pinned to its
    /// fallback for two days without the application being able to say so (issue #174).
    /// </para>
    /// <para>
    /// A default implementation is provided so adding this member does not break existing
    /// implementers of this public interface. It delegates to <see cref="GetToggleAsync"/> and
    /// reports <see cref="ConfigurationSource.Unknown"/> rather than inventing a provenance it
    /// cannot know. The built-in implementation overrides it with the real source.
    /// </para>
    /// </remarks>
    async ValueTask<ConfigurationResult<bool>> GetToggleResultAsync(string key, bool fallback = false, TimeSpan? ttl = null, string application = "")
    {
        var value = await GetToggleAsync(key, fallback, ttl, application);
        return new ConfigurationResult<bool> { Value = value, Source = ConfigurationSource.Unknown, Stale = false };
    }
}
