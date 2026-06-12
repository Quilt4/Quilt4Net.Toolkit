using Quilt4Net.Toolkit.Features.Content;
using Radzen;

namespace Quilt4Net.Toolkit.Blazor;

internal sealed class Quilt4DialogService : IQuilt4DialogService
{
    private readonly DialogService _dialogService;
    private readonly IContentService _contentService;
    private readonly ILanguageStateService _languageStateService;

    public Quilt4DialogService(DialogService dialogService, IContentService contentService, ILanguageStateService languageStateService)
    {
        _dialogService = dialogService;
        _contentService = contentService;
        _languageStateService = languageStateService;
    }

    public async Task<bool?> ConfirmAsync(string messageKey, string defaultMessage, string titleKey = null, string defaultTitle = null)
    {
        var (message, title) = await ResolveAsync(messageKey, defaultMessage, titleKey, defaultTitle);
        return await _dialogService.Confirm(message, title);
    }

    public async Task AlertAsync(string messageKey, string defaultMessage, string titleKey = null, string defaultTitle = null)
    {
        var (message, title) = await ResolveAsync(messageKey, defaultMessage, titleKey, defaultTitle);
        await _dialogService.Alert(message, title);
    }

    private async Task<(string Message, string Title)> ResolveAsync(string messageKey, string defaultMessage, string titleKey, string defaultTitle)
    {
        var message = await PlaceholderResolver.ResolveAsync(_contentService, _languageStateService, messageKey, defaultMessage);
        var title = await PlaceholderResolver.ResolveAsync(_contentService, _languageStateService, titleKey, defaultTitle);
        return (message, title);
    }
}
