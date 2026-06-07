namespace Quilt4Net.Toolkit.Features.Content.Pages;

public sealed record ContentSectionDto
{
    public string Id { get; init; }
    public int Order { get; init; }
    public string Title { get; init; }
    public SectionLayout Layout { get; init; }
    public IReadOnlyList<ContentElementDto> Elements { get; init; } = [];
}
