using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IRemoteContentCallService
{
    Task<string> GetContentAsync(string key, string defaultValue, ContentFormat contentType);
    Task SetContentAsync(string key, string defaultValue, ContentFormat contentType);
}