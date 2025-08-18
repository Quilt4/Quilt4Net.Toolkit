namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record GetContentRequest : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required string Language { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string DefaultValue { get; init; }
    public required ContentFormat ContentFormat { get; init; }
}