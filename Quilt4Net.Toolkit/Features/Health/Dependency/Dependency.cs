namespace Quilt4Net.Toolkit.Features.Health.Dependency;

public record Dependency
{
    public required string Name { get; init; }
    public bool Essential { get; init; } = true;
    public required Uri Uri { get; init; }
}