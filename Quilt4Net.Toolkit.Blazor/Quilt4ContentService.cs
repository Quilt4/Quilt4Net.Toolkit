using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Blazor;

internal class Quilt4ContentService : IQuilt4ContentService
{
    private readonly IContentService _contentService;
    private readonly ILanguageStateService _languageStateService;

    public Quilt4ContentService(IContentService contentService, ILanguageStateService languageStateService)
    {
        _contentService = contentService;
        _languageStateService = languageStateService;
    }

    public async Task<string> GetAsync(string key, string defaultValue, string application = null)
    {
        var result = await _contentService.GetContentAsync(key, defaultValue, _languageStateService.Selected.Key, ContentFormat.String, application);
        return result.Value;
    }

    public async Task<string> GetAsync(string key, IReadOnlyDictionary<string, string> defaultsByLanguage, string application = null)
    {
        // Resolve the authoritative default for the active culture, then let a stored translation for
        // the selected language win over it inside GetContentAsync (issue #135).
        var codeDefault = LanguageDefaultResolver.Resolve(defaultsByLanguage, invariantDefault: null, key);
        var result = await _contentService.GetContentAsync(key, codeDefault, _languageStateService.Selected.Key, ContentFormat.String, application);
        return result.Value;
    }

    public async Task<string> GetAsync(string key, string defaultValue, IReadOnlyDictionary<string, string> translations, string application = null)
    {
        // Send the default-language value plus the exact per-language translations. The server stores
        // each supplied translation as authoritative and skips AI translation for those languages when
        // the key is first materialized (issue #135 server half).
        var result = await _contentService.GetContentAsync(key, defaultValue, _languageStateService.Selected.Key, ContentFormat.String, application, translations);
        return result.Value;
    }
}
