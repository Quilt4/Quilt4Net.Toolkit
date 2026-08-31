using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IRemoteContentCallService
{
    Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null);

    /// <summary>
    /// As <see cref="GetContentAsync"/>, but also reports where the value came from. Prefer this
    /// when the caller needs to tell a server value apart from a cached one or a fallback default.
    /// </summary>
    Task<ContentResult> GetContentResultAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null);
    Task SetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat contentType, string application = null);
    Task<Language[]> GetLanguagesAsync(bool forceReload);
    Task ClearContentCacheAsync();

    /// <summary>
    /// Pre-fill the local cache for an entire application + language in one bulk call (the startup
    /// warm-up). Eliminates the per-key fan-out on cold load. Best-effort: a missing endpoint (old
    /// server, 404), failure, or timeout is swallowed so the normal per-key path still serves
    /// content. <paramref name="application"/> follows the usual convention (null → resolve).
    /// </summary>
    Task WarmCacheAsync(Guid languageKey, string application = null);

    /// <summary>
    /// Warm the default language plus every language named in
    /// <see cref="ContentOptions.WarmUpLanguages"/> in one bulk call each. Used by the startup
    /// warm-up and by "Reload Content". A configured name with no matching server language is
    /// skipped with a <c>Warning</c>. Best-effort throughout, like <see cref="WarmCacheAsync"/>.
    /// </summary>
    Task WarmConfiguredLanguagesAsync(string application = null);

    /// <summary>
    /// The content lifetime the server most recently reported on a successful bulk warm-up, or
    /// <c>null</c> when none has succeeded yet. Drives the periodic re-warm interval, so the client
    /// follows the server's TTL instead of guessing at it.
    /// </summary>
    /// <remarks>
    /// A default implementation returning <c>null</c> is provided so adding this member does not
    /// break existing implementers of this public interface; a caller that gets <c>null</c> falls
    /// back to its own interval.
    /// </remarks>
    TimeSpan? ObservedContentTtl => null;

    /// <summary>
    /// Number of currently-cached content entries grouped by language key — i.e. how much content
    /// has been loaded so far per language (warm-up + lazy loads). For diagnostics/admin display.
    /// </summary>
    IReadOnlyDictionary<Guid, int> GetCacheCountsByLanguage();
}