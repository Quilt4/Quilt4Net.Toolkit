namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Wire shape for a single element inside a section. Flat record + discriminator instead of a
/// type hierarchy, matching how the server stores it. Per-type fields are nullable so unused
/// fields don't bloat the JSON payload.
/// </summary>
public sealed record ContentElementDto
{
    public string Id { get; init; }
    public int Order { get; init; }
    public ElementType Type { get; init; }

    /// <summary>The server's stored value before any rendering — preserved so editors can round-trip
    /// the source. For Headline + Quotation this is the plain text. For Text it's the raw Markdown
    /// or HTML source.</summary>
    public string Text { get; init; }

    /// <summary>For Text elements: the server-rendered HTML (Markdig if the source is Markdown,
    /// sanitised pass-through if it's HTML). Toolkit emits this as <c>MarkupString</c>.</summary>
    public string Html { get; init; }

    /// <summary>For Headline elements: the heading level (1-6, mapped to h1-h6).</summary>
    public int Level { get; init; }

    /// <summary>For Quotation elements: who the quote is attributed to.</summary>
    public string Attribution { get; init; }
}
