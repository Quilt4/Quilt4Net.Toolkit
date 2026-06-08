namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Discriminator for <see cref="ContentElementDto"/>. Server-side ContentEntity uses the same
/// values; the wire encodes them as the enum name (string). New element types added on the server
/// in later phases that the toolkit doesn't recognise round-trip as <see cref="Unknown"/> so a
/// down-version toolkit renders a placeholder instead of blanking the page.
/// </summary>
public enum ElementType
{
    Unknown = 0,
    Headline,
    Text,
    Quotation,
    Divider,
}
