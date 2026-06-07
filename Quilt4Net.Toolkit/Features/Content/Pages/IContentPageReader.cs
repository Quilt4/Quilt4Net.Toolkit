namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Toolkit-side entry point for reading content pages from Quilt4Net.Server. The Blazor reader
/// component <c>&lt;ContentPageView&gt;</c> consumes this; consumers without the Blazor toolkit
/// can also use it directly to fetch pages for custom rendering.
/// </summary>
public interface IContentPageReader
{
    /// <summary>
    /// Fetch a page by its slug. Returns <c>null</c> when no page is published at any stage in
    /// the requested language (after the server's stage + language fallback chain runs).
    /// </summary>
    /// <param name="slug">Slug path, e.g. <c>"about"</c> or <c>"docs/intro"</c>.</param>
    /// <param name="languageKey">Selected language; <see cref="Guid.Empty"/> means default
    /// language.</param>
    /// <param name="application">Optional application override; <c>null</c> uses the toolkit's
    /// configured application name.</param>
    Task<ContentPageDto> GetBySlugAsync(string slug, Guid languageKey, string application = null);

    /// <summary>
    /// Fetch every page at the runtime environment's stage + language as a flat list — the
    /// <c>&lt;ContentMenu&gt;</c> component composes the tree by <see cref="ContentMenuItemDto.ParentPageId"/>.
    /// Sections + elements are omitted from this shape to keep the payload small; use
    /// <see cref="GetBySlugAsync"/> to fetch a page's body. Returns an empty array (never null)
    /// when the team has no pages or the api key is missing.
    /// </summary>
    Task<IReadOnlyList<ContentMenuItemDto>> GetTreeAsync(Guid languageKey, string application = null);
}
