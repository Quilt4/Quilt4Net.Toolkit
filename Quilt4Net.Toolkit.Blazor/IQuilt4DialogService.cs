using Radzen;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Content-aware Confirm / Alert helpers for Radzen's <see cref="DialogService"/>. Resolves
/// message and title strings through the content service in the currently-selected language,
/// so a code-behind call can carry content keys instead of hard-coded strings or per-callsite
/// <c>GetAsync</c> plumbing.
/// </summary>
public interface IQuilt4DialogService
{
    /// <summary>
    /// Shows a confirmation dialog. <paramref name="messageKey"/> is resolved through the
    /// content service; on miss / empty / lookup failure <paramref name="defaultMessage"/>
    /// is shown. The same applies to the title (omit both to leave it blank).
    /// </summary>
    /// <returns><c>true</c> if the user confirmed, <c>false</c> if cancelled,
    /// <c>null</c> if the dialog was closed without an explicit choice.</returns>
    Task<bool?> ConfirmAsync(string messageKey, string defaultMessage, string titleKey = null, string defaultTitle = null);

    /// <summary>
    /// Shows an informational alert. Same key/default precedence as <see cref="ConfirmAsync"/>.
    /// </summary>
    Task AlertAsync(string messageKey, string defaultMessage, string titleKey = null, string defaultTitle = null);
}
