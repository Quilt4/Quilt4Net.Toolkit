using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record LanguageResponse
{
    public required Language[] Languages { get; init; }
    public required DateTime ValidTo { get; init; }
}