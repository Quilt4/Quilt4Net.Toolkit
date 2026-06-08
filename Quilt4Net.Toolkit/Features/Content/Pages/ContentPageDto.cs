namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Wire shape for a hierarchical content page. <see cref="Ancestors"/> walks from the root down
/// to the parent (not including the page itself), so a reader can drive breadcrumbs without a
/// second round-trip.
/// </summary>
public sealed record ContentPageDto
{
    public string Id { get; init; }
    public string Slug { get; init; }
    public Guid LanguageKey { get; init; }
    public string Stage { get; init; }
    public string Title { get; init; }
    public string ParentPageId { get; init; }
    public int Order { get; init; }
    public bool ShowInMenu { get; init; }
    public IReadOnlyList<ContentSectionDto> Sections { get; init; } = [];

    /// <summary>Root → parent chain (excludes <see cref="Slug"/> itself). Empty for a root page.
    /// Server resolves it once so the toolkit doesn't have to walk parents itself.</summary>
    public IReadOnlyList<ContentPageAncestorDto> Ancestors { get; init; } = [];
}

public sealed record ContentPageAncestorDto
{
    public string Slug { get; init; }
    public string Title { get; init; }
}
