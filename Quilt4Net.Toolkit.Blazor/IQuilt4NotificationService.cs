using Radzen;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Content-aware wrapper around Radzen's <see cref="NotificationService"/>. Resolves
/// summary and detail strings through the content service in the currently-selected
/// language, so notification calls in C# don't need per-callsite <c>GetAsync</c> wiring.
/// </summary>
public interface IQuilt4NotificationService
{
    /// <summary>
    /// Posts a notification. <paramref name="summaryKey"/> and (optionally)
    /// <paramref name="detailKey"/> are resolved through the content service; on miss /
    /// empty / lookup failure the corresponding default is shown.
    /// </summary>
    /// <param name="severity">Radzen severity (Info / Success / Warning / Error).</param>
    /// <param name="summaryKey">Content key for the notification's summary line.</param>
    /// <param name="defaultSummary">Fallback summary used when the key isn't set or the
    /// lookup yields nothing.</param>
    /// <param name="detailKey">Optional content key for the notification's detail line.</param>
    /// <param name="defaultDetail">Optional fallback detail.</param>
    /// <param name="duration">Display duration in milliseconds (Radzen default: 3000).</param>
    Task NotifyAsync(
        NotificationSeverity severity,
        string summaryKey,
        string defaultSummary,
        string detailKey = null,
        string defaultDetail = null,
        double duration = 3000);
}
