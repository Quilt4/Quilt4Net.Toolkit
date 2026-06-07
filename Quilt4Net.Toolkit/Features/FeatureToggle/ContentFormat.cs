namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// How a content value should be rendered. Stored on <see cref="ContentEntity"/> as the BSON
/// string representation so future additions don't break old documents.
/// </summary>
public enum ContentFormat
{
    /// <summary>Plain text. Renderers HTML-encode and preserve newlines.</summary>
    String,

    /// <summary>Sanitised HTML. Renderers pass through after allow-list checks.</summary>
    Html,

    /// <summary>
    /// CommonMark Markdown. Renderers run the value through Markdig with raw-HTML disabled —
    /// safer than Html, more expressive than String. Recommended for prose-bearing snippets.
    /// </summary>
    Markdown
}