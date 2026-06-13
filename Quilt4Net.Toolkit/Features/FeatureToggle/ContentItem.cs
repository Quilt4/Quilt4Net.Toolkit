namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>A single resolved content entry (key and rendered value) within a <see cref="GetAllContentResponse"/>.</summary>
public record ContentItem
{
    /// <summary>Content key.</summary>
    public required string Key { get; init; }

    /// <summary>Rendered content value for the requested language (server-rendered, same as the single-key path).</summary>
    public required string Value { get; init; }
}
