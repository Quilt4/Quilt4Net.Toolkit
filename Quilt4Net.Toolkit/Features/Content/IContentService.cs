using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

public interface IContentService
{
    Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType);
    Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType);
}