namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record GetContentResponse
{
    public required string Value { get; init; }
    public required DateTime ValidTo { get; init; }
}