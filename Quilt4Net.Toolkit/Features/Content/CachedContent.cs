namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// A local cache entry. Distinct from the wire DTO <c>GetContentResponse</c> because the cache also
/// holds negative entries — the caller's default written after a 404, a timeout or an error, so the
/// key isn't re-requested (and re-logged) on every render.
/// </summary>
/// <remarks>
/// <see cref="IsDefault"/> is what stops a negative entry being reported as a genuine cache hit on
/// the next read. Without it, an unseeded key looks identical to real server content from the
/// second render onwards — which would make the source indicator lie in exactly the case it exists
/// to diagnose.
/// </remarks>
internal record CachedContent
{
    /// <summary>The cached value.</summary>
    public required string Value { get; init; }

    /// <summary>When this entry stops being fresh.</summary>
    public required DateTime ValidTo { get; init; }

    /// <summary>True when this entry holds a fallback default rather than a value from the server.</summary>
    public bool IsDefault { get; init; }
}
