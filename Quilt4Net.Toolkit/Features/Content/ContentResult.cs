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

    /// <summary>
    /// The language <see cref="Value"/> is actually in. <c>Guid.Empty</c> is the default language;
    /// <c>null</c> when the server did not report it.
    /// </summary>
    public Guid? ServedLanguageKey { get; init; }

    /// <summary>
    /// Why <see cref="Value"/> is not in the requested language, when it is not.
    /// <see cref="ContentFallbackReason.Unknown"/> against a server that predates the field.
    /// </summary>
    public ContentFallbackReason FallbackReason { get; init; }

    /// <summary>
    /// True when the value came from a lower stage than this environment maps to. Orthogonal to
    /// <see cref="FallbackReason"/> — a value can be both.
    /// </summary>
    public bool IsStageFallback { get; init; }

    /// <summary>
    /// Whether asking again later could yield a better answer: <c>true</c> only while a translation
    /// is queued, <c>false</c> for a dead end, and <c>null</c> when the server did not say.
    /// </summary>
    /// <remarks>
    /// Derived rather than carried on the wire, so it can never disagree with
    /// <see cref="FallbackReason"/>. Three-valued on purpose — an older server reports nothing, and
    /// "no better result is coming" is a meaningfully different claim from "I don't know", which a
    /// plain <c>bool</c> would quietly conflate.
    /// </remarks>
    public bool? CanImprove => FallbackReason switch
    {
        ContentFallbackReason.Unknown => null,
        ContentFallbackReason.TranslationPending => true,
        _ => false,
    };
}
