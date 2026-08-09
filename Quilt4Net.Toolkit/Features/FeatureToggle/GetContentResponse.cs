using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record GetContentResponse
{
    public required string Value { get; init; }
    public required DateTime ValidTo { get; init; }

    /// <summary>
    /// The language <see cref="Value"/> is actually in, which is not necessarily the one requested.
    /// <c>Guid.Empty</c> is the default language; <c>null</c> means the server did not report it
    /// (older server).
    /// </summary>
    /// <remarks>
    /// Optional on purpose — every metadata field here is. The server populates them only from the
    /// release that added them, and a client on an older server must degrade to "unknown" rather
    /// than to a confident wrong answer. That is also why
    /// <see cref="ContentFallbackReason.Unknown"/> is the enum's zero value.
    /// </remarks>
    public Guid? ServedLanguageKey { get; init; }

    /// <summary>Why <see cref="Value"/> is not in the requested language, when it is not.</summary>
    public ContentFallbackReason FallbackReason { get; init; }

    /// <summary>
    /// True when the value was served from a lower stage than the caller's environment maps to.
    /// <b>Orthogonal</b> to <see cref="FallbackReason"/> — a value can be both from a lower stage
    /// and in the wrong language, so the two dimensions are reported separately rather than
    /// collapsed into one enum.
    /// </summary>
    public bool IsStageFallback { get; init; }
}
