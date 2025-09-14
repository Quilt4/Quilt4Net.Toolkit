﻿namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface ILanguageKeyContext
{
    string Key { get; }
    Guid LanguageKey { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
}

public record LanguageKeyContext : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required Guid LanguageKey { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
}