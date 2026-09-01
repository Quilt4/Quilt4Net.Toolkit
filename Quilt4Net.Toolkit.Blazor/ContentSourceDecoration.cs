using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Shared styling for the content source overlay, so every component annotates a given
/// <see cref="ContentSource"/> the same way.
/// </summary>
/// <remarks>
/// <para>
/// Colour carries meaning here: red marks a fallback default — the case you are usually hunting,
/// because it means the server has no value for that key. Green is a fresh server value, blue a
/// cache hit, amber a stale one. The outline shape matches the existing edit-mode decoration so the
/// two modes look related.
/// </para>
/// <para>
/// A <b>language</b> fallback is a second, independent dimension: the value can be a perfectly fresh
/// server hit and still be the wrong language. That is drawn as a <b>dashed</b> outline rather than
/// its own colour, so the source colour keeps meaning what it always did and the two read together.
/// </para>
/// </remarks>
internal static class ContentSourceDecoration
{
    public static string Style(ContentSource source)
    {
        var colour = source switch
        {
            ContentSource.Server => "#2e7d32",     // green — fetched now
            ContentSource.Cache => "#1565c0",      // blue — cached server value
            ContentSource.StaleCache => "#ef6c00", // amber — cached but past TTL
            ContentSource.Default => "#c62828",    // red — fallback, server has no value
            ContentSource.Developer => "#6a1b9a",  // purple — a developer language is active ("X" or "Key")
            ContentSource.NoApiKey => "#546e7a",   // grey — no lookup attempted
            _ => "#546e7a"                         // grey — provenance not reported
        };
        return $"outline: 2px solid {colour}; outline-offset: -4px;";
    }

    /// <summary>
    /// As <see cref="Style(ContentSource)"/>, but dashes the outline when the value is not in the
    /// requested language. Falls through to the source-only style when the server reported nothing
    /// (<see cref="ContentFallbackReason.Unknown"/>) — an older server must not make every value
    /// look like a fallback.
    /// </summary>
    public static string Style(ContentResult result)
    {
        var style = Style(result.Source);
        return IsLanguageFallback(result) ? style.Replace("solid", "dashed") : style;
    }

    public static string Tooltip(ContentSource source)
    {
        return source switch
        {
            ContentSource.Server => "Source: server (fetched on this render)",
            ContentSource.Cache => "Source: local cache (value from the server)",
            ContentSource.StaleCache => "Source: local cache, past its TTL (refreshing in the background)",
            ContentSource.Default => "Source: fallback default — the server has no value for this key",
            ContentSource.Developer => "Source: developer language — the placeholder 'X', or the key itself",
            ContentSource.NoApiKey => "Source: fallback default — no API key configured, no lookup attempted",
            _ => "Source: not reported by this IContentService implementation"
        };
    }

    /// <summary>
    /// As <see cref="Tooltip(ContentSource)"/>, plus a line explaining the language fallback and —
    /// the point of the whole thing — whether asking again later could do better.
    /// </summary>
    public static string Tooltip(ContentResult result)
    {
        var tooltip = Tooltip(result.Source);

        if (result.IsStageFallback) tooltip += "\nStage: served from a lower stage than this environment maps to.";
        if (!IsLanguageFallback(result)) return tooltip;

        var reason = result.FallbackReason switch
        {
            // The only reason for which waiting is the right move.
            ContentFallbackReason.TranslationPending =>
                "Language: not translated yet — a translation is queued, so a later load may show it.",
            ContentFallbackReason.TranslationFailed =>
                "Language: translation failed and gave up. Waiting will not help — requeue it or write the text.",
            ContentFallbackReason.TranslationDisabled =>
                "Language: machine translation is off for this language. It has to be authored.",
            ContentFallbackReason.NoContent =>
                "Language: the key has no stored content at all — this is the caller's own default.",
            _ => null,
        };

        return reason == null ? tooltip : $"{tooltip}\n{reason}";
    }

    /// <summary>
    /// Whether the value is in a language other than the one asked for. <c>Unknown</c> and
    /// <c>None</c> both mean "do not decorate" — the first because the server said nothing, the
    /// second because there is nothing to say.
    /// </summary>
    private static bool IsLanguageFallback(ContentResult result)
        => result.FallbackReason is not (ContentFallbackReason.Unknown or ContentFallbackReason.None);
}
