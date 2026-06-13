namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// Bulk content response used by the startup warm-up. Carries every resolved content value for a
/// single application + environment + language in one round-trip, sharing one <see cref="ValidTo"/>
/// so the client can seed its cache without a call per key.
/// </summary>
public record GetAllContentResponse
{
    /// <summary>The resolved content entries (already rendered server-side, same as the single-key path).</summary>
    public required ContentItem[] Items { get; init; }

    /// <summary>Cache expiry shared by every entry in <see cref="Items"/>.</summary>
    public required DateTime ValidTo { get; init; }
}
