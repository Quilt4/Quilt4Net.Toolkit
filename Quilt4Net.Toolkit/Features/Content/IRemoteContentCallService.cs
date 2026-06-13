using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IRemoteContentCallService
{
    Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null);
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
    /// Number of currently-cached content entries grouped by language key — i.e. how much content
    /// has been loaded so far per language (warm-up + lazy loads). For diagnostics/admin display.
    /// </summary>
    IReadOnlyDictionary<Guid, int> GetCacheCountsByLanguage();
}