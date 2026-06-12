using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Shared content-lookup helper for the placeholder-wrapping Quilt4Radzen* components
/// (<see cref="Quilt4RadzenTextBox"/>, <see cref="Quilt4RadzenTextArea"/>,
/// <see cref="Quilt4RadzenDropDown{TValue}"/>, <see cref="Quilt4RadzenNumeric{TValue}"/>).
/// Each component has its own pass-through param surface but the placeholder resolution
/// is identical, so this lives in one place.
/// </summary>
internal static class PlaceholderResolver
{
    /// <summary>
    /// Resolves <paramref name="key"/> via the content service in the currently-selected
    /// language. Falls through to <paramref name="default"/> when the key is null/empty,
    /// the service returns an empty value, or the lookup fails. Matches the precedence the
    /// other Quilt4Radzen* wrappers (DataGridColumn / PanelMenuItem) already use.
    /// </summary>
    public static async Task<string> ResolveAsync(
        IContentService contentService,
        ILanguageStateService languageState,
        string key,
        string @default)
    {
        if (string.IsNullOrEmpty(key)) return @default;
        var response = await contentService.GetContentAsync(
            key, @default, languageState.Selected?.Key ?? Guid.Empty, ContentFormat.String);
        return !string.IsNullOrEmpty(response.Value) ? response.Value : @default;
    }
}
