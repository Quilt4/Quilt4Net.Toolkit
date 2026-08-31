namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// A resolved configuration value together with its provenance.
/// </summary>
/// <typeparam name="T">The value type the caller asked for.</typeparam>
/// <remarks>
/// Use this where a fallback must be distinguishable from a real answer — logging why a feature is
/// off, refusing to act on a value the server never confirmed, or surfacing "degraded" in a health
/// check. The plain read remains the right call when the fallback is genuinely as good as the
/// answer.
/// </remarks>
public record ConfigurationResult<T>
{
    /// <summary>The resolved value. Falls back to the caller's fallback when nothing else is known.</summary>
    public required T Value { get; init; }

    /// <summary>Where <see cref="Value"/> came from.</summary>
    public required ConfigurationSource Source { get; init; }

    /// <summary>
    /// True when the value is not known to be current — a stale cache entry, or a fallback standing
    /// in for a value the server never confirmed.
    /// </summary>
    public required bool Stale { get; init; }
}
