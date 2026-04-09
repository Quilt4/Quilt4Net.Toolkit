using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IRemoteContentCallService
{
    Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null);
    Task SetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat contentType, string application = null);
    Task<Language[]> GetLanguagesAsync(bool forceReload);
    Task ClearContentCacheAsync();
}