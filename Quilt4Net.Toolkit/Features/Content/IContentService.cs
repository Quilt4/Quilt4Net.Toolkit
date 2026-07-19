using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IContentService
{
    Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null);

    /// <summary>
    /// As <see cref="GetContentAsync"/>, but also reports where the value came from
    /// (<see cref="ContentSource"/>) — server, cache, stale cache or fallback default.
    /// </summary>
    /// <remarks>
    /// A default implementation is provided so adding this member does not break existing
    /// implementers of this public interface. It delegates to <see cref="GetContentAsync"/> and
    /// reports <see cref="ContentSource.Unknown"/> rather than inventing a provenance it cannot
    /// know. The built-in implementation overrides it with the real source.
    /// </remarks>
    async Task<ContentResult> GetContentResultAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null)
    {
        var (value, success) = await GetContentAsync(key, defaultValue, languageKey, contentType, application, translations);
        return new ContentResult { Value = value, Success = success, Source = ContentSource.Unknown, Stale = !success };
    }
    Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType, string application = null);
    Task ClearCacheAsync();
}