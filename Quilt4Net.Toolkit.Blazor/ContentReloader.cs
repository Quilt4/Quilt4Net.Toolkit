using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// The admin "Reload Content" sequence, shared by <c>LanguageSelector</c> and <c>ContentAdmin</c>:
/// reload the language list, flush the content cache, then re-warm the configured languages. The
/// caller forces the page reload afterwards.
/// </summary>
/// <remarks>
/// The re-warm is what makes this a hot-load rather than a cache flush — the cache lives in the
/// singleton <see cref="IRemoteContentCallService"/> and the startup
/// <see cref="ContentWarmupHostedService"/> only runs once per process, so nothing refills it after
/// a clear. Without the re-warm the cleared cache repopulates lazily, one key at a time, as pages
/// render. Both entry points go through here so they cannot drift apart again.
/// </remarks>
internal static class ContentReloader
{
    public static async Task ReloadAsync(ILanguageStateService languageStateService, IContentService contentService, IRemoteContentCallService remoteContentCallService)
    {
        await languageStateService.ReloadAsync();
        await contentService.ClearCacheAsync();

        // Re-warm before the caller's forced reload so the new circuit renders from a warm cache (the
        // singleton cache persists across circuits). Best-effort — a warm failure must not block the
        // reload.
        try
        {
            await remoteContentCallService.WarmConfiguredLanguagesAsync();
        }
        catch
        {
            // Ignored: the per-key path still serves content on the reload.
        }
    }
}
