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
    /// <summary>
    /// Resolve many keys at once, returning a dictionary keyed by content key with the caller's own
    /// default filled in for anything unresolved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The warm-up path has always fetched in bulk; callers had no equivalent, so a view declaring
    /// many keys resolved them one at a time. One consuming dialog has 42 call sites plus four
    /// option builders that <c>await</c> once per enum value inside a <c>foreach</c> — around 70
    /// sequential calls to open one dialog (Toolkit issue #183).
    /// </para>
    /// <para>
    /// The per-key path remains for one-offs. This is an addition, not a replacement.
    /// </para>
    /// <para>
    /// A default implementation is provided so adding this member does not break existing
    /// implementers of this public interface; it simply loops the single-key path, which is what a
    /// caller would otherwise have written by hand.
    /// </para>
    /// </remarks>
    async Task<IReadOnlyDictionary<string, string>> GetManyContentAsync(IReadOnlyCollection<ContentRequest> requests, Guid languageKey, ContentFormat? contentType, string application = null)
    {
        var result = new Dictionary<string, string>();
        foreach (var request in requests ?? [])
        {
            var (value, _) = await GetContentAsync(request.Key, request.DefaultValue, languageKey, contentType, application, request.Translations);
            result[request.Key] = value;
        }
        return result;
    }

    Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType, string application = null);
    Task ClearCacheAsync();
}