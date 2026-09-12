namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Authored form of one element inside a <see cref="ContentSectionWrite"/>. Which payload fields
/// are meaningful depends on <see cref="Type"/>, exactly as on the read side.
/// </summary>
/// <remarks>
/// The difference from <see cref="ContentElementDto"/> is <see cref="Body"/>. The read DTO carries
/// <c>Html</c>, rendered server-side from Markdown so a renderer does not need a Markdown library;
/// this carries the Markdown itself. Writing back the rendered HTML would replace an author's
/// source with its own output, and the next edit would be of HTML rather than of Markdown.
/// </remarks>
public sealed record ContentElementWrite
{
    /// <summary>
    /// Stable identifier within the section. <b>Optional</b> — derived deterministically from
    /// position when absent, for the reason given on <see cref="ContentSectionWrite.Id"/>.
    /// </summary>
    public string Id { get; init; }

    /// <summary>Render order within the section. Sorted ascending; gaps are fine.</summary>
    public required int Order { get; init; }

    /// <summary>Which payload fields below are read.</summary>
    public required ElementType Type { get; init; }

    /// <summary>Heading depth 1..6. <see cref="ElementType.Headline"/> only.</summary>
    public int Level { get; init; }

    /// <summary>Display text, for <see cref="ElementType.Headline"/> and
    /// <see cref="ElementType.Quotation"/>.</summary>
    public string Text { get; init; }

    /// <summary>Markdown source, for <see cref="ElementType.Text"/> only. Rendered to HTML on read
    /// with raw HTML disabled, so markup embedded here is escaped rather than honoured.</summary>
    public string Body { get; init; }

    /// <summary>Optional attribution on a <see cref="ElementType.Quotation"/>.</summary>
    public string Attribution { get; init; }
}
