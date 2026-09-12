namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>What a <see cref="ContentImportRequest"/> actually did.</summary>
public sealed record ContentImportResult
{
    /// <summary>How many keys the request touched.</summary>
    public int KeysWritten { get; init; }

    /// <summary>How many individual language values were written across those keys.</summary>
    public int ValuesWritten { get; init; }

    /// <summary>
    /// Language names in the payload that match no configured language, and were therefore skipped.
    /// </summary>
    /// <remarks>
    /// Reported rather than silently dropped. An unrecognised name is almost always a typo or a
    /// display name that has since been renamed — <c>"Svenska"</c> where the server calls it
    /// <c>"Swedish"</c> — and the failure is otherwise invisible: the call returns success, and the
    /// translation simply never appears. A pipeline can fail its own build on a non-empty list here.
    /// </remarks>
    public IReadOnlyList<string> IgnoredLanguages { get; init; } = [];
}
