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
}
