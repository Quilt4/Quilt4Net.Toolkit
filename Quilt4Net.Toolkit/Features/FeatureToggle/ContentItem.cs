using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>A single resolved content entry (key and rendered value) within a <see cref="GetAllContentResponse"/>.</summary>
public record ContentItem
{
    /// <summary>Content key.</summary>
    public required string Key { get; init; }

    /// <summary>Rendered content value for the requested language (server-rendered, same as the single-key path).</summary>
    public required string Value { get; init; }

    /// <summary>
    /// The language <see cref="Value"/> is actually in. <c>Guid.Empty</c> is the default language;
    /// <c>null</c> means the server did not report it.
    /// </summary>
    /// <remarks>
    /// The bulk path carries the same metadata as the single-key path deliberately. Warm-up is on by
    /// default, so most rendered content arrives through here — omit it and the fallback indicator
    /// would be blank for nearly everything, appearing only after a cache entry expired.
    /// </remarks>
    public Guid? ServedLanguageKey { get; init; }

    /// <summary>Why <see cref="Value"/> is not in the requested language, when it is not.</summary>
    public ContentFallbackReason FallbackReason { get; init; }

    /// <summary>True when the value came from a lower stage than the caller's environment maps to.</summary>
    public bool IsStageFallback { get; init; }
}
