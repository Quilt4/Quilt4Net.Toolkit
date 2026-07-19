namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// A resolved content value together with its provenance.
/// </summary>
/// <remarks>
/// The older <c>(string Value, bool Success)</c> tuple is kept for backward compatibility, but its
/// <c>Success</c> flag is not a source discriminator — it is <c>true</c> for a cache hit, a stale
/// cache hit and a fresh server fetch alike, and <c>false</c> for a missing API key, a 404, a
/// timeout and an exception alike. Use <see cref="Source"/> when the distinction matters.
/// </remarks>
public record ContentResult
{
    /// <summary>The resolved value. Never null — falls back to the caller's default.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// Whether the lookup path completed without falling back. Retained to match the legacy tuple;
    /// prefer <see cref="Source"/> for anything finer-grained.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>Where <see cref="Value"/> came from.</summary>
    public required ContentSource Source { get; init; }

    /// <summary>
    /// True when the value is not known to be current — a stale cache entry, or a default standing
    /// in for a value the server never confirmed.
    /// </summary>
    public required bool Stale { get; init; }
}
