namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// Where a resolved configuration value actually came from. The configuration counterpart of
/// <see cref="Quilt4Net.Toolkit.Features.Content.ContentSource"/>.
/// </summary>
/// <remarks>
/// Issue #174: <c>GetToggleAsync("X", false)</c> returning <c>false</c> cannot be told apart from a
/// server that genuinely says <c>false</c>, so an application pinned to its fallback by a sustained
/// fault looks exactly like one that is switched off on purpose. The distinction was always known
/// inside the client — it went to the Debug log and no further.
/// </remarks>
public enum ConfigurationSource
{
    /// <summary>
    /// The caller's fallback was used — no value has ever been resolved for the key, and the server
    /// was unreachable, timed out or refused the call.
    /// </summary>
    Fallback,

    /// <summary>Served from the local cache, within its lifetime, from a value the server supplied.</summary>
    Cache,

    /// <summary>
    /// Served from the local cache after its lifetime expired, with a background refresh started
    /// (stale-while-revalidate). The value came from the server, but may be out of date.
    /// </summary>
    StaleCache,

    /// <summary>Fetched from Quilt4Net.Server on this call.</summary>
    Server,

    /// <summary>
    /// The implementation does not report provenance. Returned by the default interface
    /// implementation, so a custom or test implementation that only overrides the plain read
    /// reports "unknown" rather than guessing.
    /// </summary>
    Unknown
}
