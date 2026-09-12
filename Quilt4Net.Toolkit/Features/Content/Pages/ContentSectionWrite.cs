namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>Authored form of one section on a <see cref="ContentPageWrite"/>.</summary>
public sealed record ContentSectionWrite
{
    /// <summary>
    /// Stable identifier within the page. <b>Optional.</b>
    /// </summary>
    /// <remarks>
    /// A repository-authored page usually has no ids to give — the source is Markdown in a file,
    /// not a document with keys. When this is null the server derives a deterministic id from the
    /// section's position in the page, so re-pushing the same source produces the same ids and the
    /// write stays idempotent. Generating a fresh id per push would rewrite every section on every
    /// deploy and defeat that.
    ///
    /// Supply an id when one already exists (read it back from <see cref="ContentSectionDto.Id"/>)
    /// and you want that section's identity preserved across a reorder.
    /// </remarks>
    public string Id { get; init; }

    /// <summary>Render order within the page. Sorted ascending; gaps are fine.</summary>
    public required int Order { get; init; }

    /// <summary>Optional section heading, rendered above the elements.</summary>
    public string Title { get; init; }

    /// <summary>Inner layout of the section's elements.</summary>
    public SectionLayout Layout { get; init; }

    /// <summary>The section's elements. An empty list is valid — a section with only a title is
    /// meaningful visual structure.</summary>
    public required IReadOnlyList<ContentElementWrite> Elements { get; init; }
}
