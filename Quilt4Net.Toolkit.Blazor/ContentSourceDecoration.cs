using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Shared styling for the content source overlay, so every component annotates a given
/// <see cref="ContentSource"/> the same way.
/// </summary>
/// <remarks>
/// Colour carries meaning here: red marks a fallback default — the case you are usually hunting,
/// because it means the server has no value for that key. Green is a fresh server value, blue a
/// cache hit, amber a stale one. The outline shape matches the existing edit-mode decoration so the
/// two modes look related.
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
            ContentSource.Developer => "#6a1b9a",  // purple — developer language active
            ContentSource.NoApiKey => "#546e7a",   // grey — no lookup attempted
            _ => "#546e7a"                         // grey — provenance not reported
        };
        return $"outline: 2px solid {colour}; outline-offset: -4px;";
    }

    public static string Tooltip(ContentSource source)
    {
        return source switch
        {
            ContentSource.Server => "Source: server (fetched on this render)",
            ContentSource.Cache => "Source: local cache (value from the server)",
            ContentSource.StaleCache => "Source: local cache, past its TTL (refreshing in the background)",
            ContentSource.Default => "Source: fallback default — the server has no value for this key",
            ContentSource.Developer => "Source: developer language placeholder",
            ContentSource.NoApiKey => "Source: fallback default — no API key configured, no lookup attempted",
            _ => "Source: not reported by this IContentService implementation"
        };
    }
}
