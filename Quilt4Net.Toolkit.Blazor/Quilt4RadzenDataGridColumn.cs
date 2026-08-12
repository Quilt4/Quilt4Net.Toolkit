using Microsoft.AspNetCore.Components;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Radzen.Blazor;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// A <see cref="RadzenDataGridColumn{TItem}"/> whose title is resolved from Quilt4Net content by key,
/// falling back to <see cref="DefaultTitle"/>, and re-resolved live on language change.
/// </summary>
/// <remarks>
/// <para>
/// This <b>inherits</b> the Radzen column rather than wrapping one (#162). A wrapper is a component in its
/// own right, so the column it renders is created a render generation later than a plain
/// <c>RadzenDataGridColumn</c> declared beside it — and the grid collects columns in the order they
/// register, not the order they are written. A grid mixing wrapped and plain columns therefore rendered
/// the plain ones first, silently, whatever the markup said.
/// </para>
/// <para>
/// Inheriting also means every Radzen column parameter works here, not just the handful the old wrapper
/// forwarded: <c>OrderIndex</c>, <c>Frozen</c>, <c>Groupable</c>, <c>HeaderTemplate</c>, the filter surface
/// and the rest all behave exactly as they do on a plain column.
/// </para>
/// </remarks>
public class Quilt4RadzenDataGridColumn<TItem> : RadzenDataGridColumn<TItem>, IDisposable
{
    private EventHandler<LanguageChangedEventArgs> _languageChanged;

    [Inject] private IContentService ContentService { get; set; }

    [Inject] private ILanguageStateService LanguageStateService { get; set; }

    /// <summary>Content key for the column title.</summary>
    [Parameter]
    public string TitleKey { get; set; }

    /// <summary>Title used until content resolves, and whenever the lookup misses or fails.</summary>
    [Parameter]
    public string DefaultTitle { get; set; }

    /// <summary>Optional exact translations for <see cref="TitleKey"/>, keyed by <b>language name</b>
    /// exactly as entered on the server (e.g. <c>{ ["Svenska"] = "Ärende" }</c>). Applied only the
    /// first time the key is created on the server: each is stored as authoritative for the matching
    /// language and AI translation is skipped for it (issue #141). Optional — omit for the ordinary
    /// key + default flow.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string> Translations { get; set; }

    /// <summary>The title currently resolved from content. Exposed for tests.</summary>
    internal string LoadedTitle { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Held in a field so it can be detached again. The wrapper subscribed with an anonymous lambda and
        // never unsubscribed; as a column this instance lives for as long as the grid does, so the leak
        // would now outlast every re-render.
        _languageChanged = async (_, _) =>
        {
            await LoadContentAsync();
            await InvokeAsync(StateHasChanged);
        };
        LanguageStateService.LanguageChangedEvent += _languageChanged;

        await LoadContentAsync();
    }

    private async Task LoadContentAsync()
    {
        LoadedTitle = await PlaceholderResolver.ResolveAsync(
            ContentService, LanguageStateService, TitleKey, DefaultTitle, Translations);

        // SetTitle rather than assigning Title: the grid reads the title through GetTitle(), and this is
        // the same path Radzen's own column picker uses to rename a column at runtime.
        SetTitle(LoadedTitle);
    }

    // Radzen's Dispose is public but not virtual, so it cannot be overridden. Re-implementing IDisposable
    // explicitly re-maps the interface for this type, and the renderer disposes a component through
    // `IDisposable` — so this runs, then hands off to Radzen's own de-registration.
    void IDisposable.Dispose()
    {
        if (_languageChanged != null)
        {
            LanguageStateService.LanguageChangedEvent -= _languageChanged;
            _languageChanged = null;
        }

        base.Dispose();
    }
}
