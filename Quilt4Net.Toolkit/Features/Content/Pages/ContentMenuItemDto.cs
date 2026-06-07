namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Lightweight wire shape for the menu tree — sections and elements are intentionally omitted so a
/// site with hundreds of pages doesn't ship its entire content body just to draw the nav menu.
/// </summary>
public sealed record ContentMenuItemDto
{
    public string Id { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
    public string ParentPageId { get; init; }
    public int Order { get; init; }
    public bool ShowInMenu { get; init; }
}
