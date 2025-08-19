using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

internal class ContentService : IContentService
{
    private readonly IRemoteContentCallService _remoteContentCallService;

    public ContentService(IRemoteContentCallService remoteContentCallService)
    {
        _remoteContentCallService = remoteContentCallService;
    }

    public Task<string> GetContentAsync(string key, string defaultValue, ContentFormat? contentType)
    {
        return _remoteContentCallService.GetContentAsync(key, defaultValue, contentType);
    }

    public Task SetContentAsync(string key, string value, ContentFormat contentType)
    {
        return _remoteContentCallService.SetContentAsync(key, value, contentType);
    }
}