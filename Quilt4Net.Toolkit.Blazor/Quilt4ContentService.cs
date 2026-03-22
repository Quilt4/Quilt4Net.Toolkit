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

    public async Task<string> GetAsync(string key, string defaultValue)
    {
        var result = await _contentService.GetContentAsync(key, defaultValue, _languageStateService.Selected.Key, ContentFormat.String);
        return result.Value;
    }
}
