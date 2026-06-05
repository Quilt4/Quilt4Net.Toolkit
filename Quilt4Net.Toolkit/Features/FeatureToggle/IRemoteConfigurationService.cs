namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// Reads — and, for admin scenarios, writes — remote configuration values and feature toggles served by
/// Quilt4Net. Read results are cached locally with stale-while-revalidate; a failed server call falls back
/// to the last known value or the supplied <c>fallback</c>, so reads never throw.
/// </summary>
public interface IRemoteConfigurationService
{
    /// <summary>
    /// Resolves a typed remote configuration value.
    /// </summary>
    /// <typeparam name="T">
    /// Value type. Must be convertible from its string representation (e.g. <see cref="bool"/>,
    /// <see cref="int"/>, <see cref="string"/>).
    /// </typeparam>
    /// <param name="key">Configuration key.</param>
    /// <param name="fallback">Value returned when the key is unknown or the server is unreachable.</param>
    /// <param name="ttl">
    /// Optional client-requested cache lifetime. When <c>null</c>, the team/server-configured default applies.
    /// </param>
    /// <param name="application">
    /// Which application's value to read:
    /// <list type="bullet">
    /// <item><description><b>Empty string (the default)</b> — the <b>shared</b>, cross-application value.</description></item>
    /// <item><description><c>null</c> — the value for the configured <see cref="RemoteConfigurationOptions.Application"/>, or the entry assembly name when that is unset (i.e. "this application").</description></item>
    /// <item><description>A specific name — the value for that named application.</description></item>
    /// </list>
    /// </param>
    ValueTask<T> GetAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = "");

    /// <summary>
    /// Resolves a boolean feature toggle — shorthand for <see cref="GetAsync{T}"/> with <c>T</c> = <see cref="bool"/>.
    /// </summary>
    /// <param name="key">Toggle key.</param>
    /// <param name="fallback">Value returned when the key is unknown or the server is unreachable.</param>
    /// <param name="ttl">
    /// Optional client-requested cache lifetime. When <c>null</c>, the team/server-configured default applies.
    /// </param>
    /// <param name="application">
    /// Which application's toggle to read: empty string (the default) reads the <b>shared</b> value; <c>null</c>
    /// reads the configured application's value (or the entry assembly name); a specific name reads that
    /// application's value. See <see cref="GetAsync{T}"/> for details.
    /// </param>
    ValueTask<bool> GetToggleAsync(string key, bool fallback = default, TimeSpan? ttl = null, string application = "");

    /// <summary>
    /// Lists all configuration entries (feature toggles and configuration values) for the current
    /// application and environment — for admin / overview UIs.
    /// </summary>
    Task<ConfigurationResponse[]> GetAsync();

    /// <summary>
    /// Deletes a configuration entry. Requires a server API key carrying the <c>config:write</c> scope.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="application">Owning application; pass an empty string for a shared entry.</param>
    /// <param name="environment">Owning environment.</param>
    /// <param name="instance">Owning instance, or <c>null</c>.</param>
    Task DeleteAsync(string key, string application, string environment, string instance);

    /// <summary>
    /// Creates or updates a configuration value. Requires a server API key carrying the <c>config:write</c> scope.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="application">Owning application; pass an empty string for a shared entry.</param>
    /// <param name="environment">Owning environment.</param>
    /// <param name="instance">Owning instance, or <c>null</c>.</param>
    /// <param name="value">New value (stored as its string representation).</param>
    Task SetAsync(string key, string application, string environment, string instance, string value);
}
