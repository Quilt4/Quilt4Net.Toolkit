using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

internal class ContentService : IContentService
{
    private readonly IRemoteContentCallService _remoteContentCallService;

    public ContentService(IRemoteContentCallService remoteContentCallService)
    {
        _remoteContentCallService = remoteContentCallService;
    }

    public Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType)
    {
        return _remoteContentCallService.GetContentAsync(key, defaultValue, languageKey, contentType);
    }

    public Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType)
    {
        return _remoteContentCallService.SetContentAsync(key, value, languageKey, contentType);
    }

    public Task ClearCacheAsync()
    {
        return _remoteContentCallService.ClearContentCacheAsync();
    }
}