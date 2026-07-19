namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// Where a resolved content value actually came from. Surfaced so a consumer can tell a real
/// server value apart from a fallback — the distinction the toolkit has always resolved
/// internally but previously only wrote to the Debug log.
/// </summary>
public enum ContentSource
{
    /// <summary>
    /// The caller's default value was used — the server had no override for the key (404), was
    /// unreachable, or timed out. Also reported for a cached entry that holds a default rather
    /// than server content, so a negative-cache hit is never mistaken for a real cache hit.
    /// </summary>
    Default,

    /// <summary>Served from the local cache, within its TTL, from a value the server supplied.</summary>
    Cache,

    /// <summary>
    /// Served from the local cache after its TTL expired, with a background refresh started
    /// (stale-while-revalidate). The value came from the server, but may be out of date.
    /// </summary>
    StaleCache,

    /// <summary>Fetched from Quilt4Net.Server on this call.</summary>
    Server,

    /// <summary>Developer language is active — every key resolves to the placeholder text.</summary>
    Developer,

    /// <summary>No API key is configured, so no lookup was attempted and the default was used.</summary>
    NoApiKey,

    /// <summary>
    /// The <see cref="IContentService"/> implementation does not report provenance. Returned by the
    /// default interface implementation, so a custom or test implementation that only overrides
    /// <c>GetContentAsync</c> reports "unknown" rather than guessing.
    /// </summary>
    Unknown
}
