namespace Quilt4Net.Toolkit.Api.Features.Health;

public record Component
{
    public required string Status { get; init; }
    public required Dictionary<string, string> Details { get; init; }
}