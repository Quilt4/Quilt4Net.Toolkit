using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record SetContentRequest : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required Guid LanguageKey { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string Value { get; init; }
    public required ContentFormat ContentType { get; init; }
}

public record LanguageResponse
{
    public required Language[] Languages { get; init; }
    public required DateTime ValidTo { get; init; }
}