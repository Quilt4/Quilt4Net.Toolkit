using Quilt4Net.Toolkit.Features.Content;
using Radzen;

namespace Quilt4Net.Toolkit.Blazor;

internal sealed class Quilt4NotificationService : IQuilt4NotificationService
{
    private readonly NotificationService _notificationService;
    private readonly IContentService _contentService;
    private readonly ILanguageStateService _languageStateService;

    public Quilt4NotificationService(NotificationService notificationService, IContentService contentService, ILanguageStateService languageStateService)
    {
        _notificationService = notificationService;
        _contentService = contentService;
        _languageStateService = languageStateService;
    }

    public async Task NotifyAsync(
        NotificationSeverity severity,
        string summaryKey,
        string defaultSummary,
        string detailKey = null,
        string defaultDetail = null,
        double duration = 3000)
    {
        var summary = await PlaceholderResolver.ResolveAsync(_contentService, _languageStateService, summaryKey, defaultSummary);
        var detail = string.IsNullOrEmpty(detailKey) && string.IsNullOrEmpty(defaultDetail)
            ? null
            : await PlaceholderResolver.ResolveAsync(_contentService, _languageStateService, detailKey, defaultDetail);
        _notificationService.Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail,
            Duration = duration,
        });
    }
}
