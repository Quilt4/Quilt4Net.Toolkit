namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// Why a content value is not in the language that was asked for.
/// <para>
/// Distinct from <see cref="ContentSource"/>, which says where the value was fetched from (server,
/// cache, fallback default). This says <i>what the value is</i>: a request can be a fresh server hit
/// and still be the wrong language.
/// </para>
/// <para>
/// The question this exists to answer is whether calling again later could do better. Use
/// <c>ContentResult.CanImprove</c> rather than switching on this directly when that is all you need.
/// </para>
/// </summary>
public enum ContentFallbackReason
{
    /// <summary>
    /// The server did not report a reason. Means "not known", <b>not</b> "no fallback" — an older
    /// server omits the field entirely, so this is what every value from one deserializes to.
    /// Deliberately the zero value so that absence and ignorance read the same.
    /// </summary>
    Unknown = 0,

    /// <summary>The value is in the requested language. No language fallback happened.</summary>
    None,

    /// <summary>
    /// No value in the requested language yet, and a translation is queued. **Trying again later may
    /// succeed** — this is the only reason for which that is true.
    /// </summary>
    TranslationPending,

    /// <summary>
    /// A translation was attempted and gave up (its retry budget is spent). Waiting will not help;
    /// somebody has to requeue it or write the text.
    /// </summary>
    TranslationFailed,

    /// <summary>
    /// The language is not machine-translated (AI translation is off for it), so nothing will ever
    /// produce this value automatically. It must be authored.
    /// </summary>
    TranslationDisabled,

    /// <summary>
    /// The key has no stored content at all — the value being rendered is the caller's own default,
    /// echoed back. Nothing is queued because there is no source text to translate.
    /// </summary>
    NoContent,
}
