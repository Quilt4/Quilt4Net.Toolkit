namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public record FeatureToggleResponse
{
    public required string Value { get; init; }
}