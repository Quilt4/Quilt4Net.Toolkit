namespace Quilt4Net.Toolkit.Features.ReleaseNotes;

/// <summary>
/// Everything published after a version the caller names — the answer to "what has happened since
/// this user was last here". Newest first.
/// </summary>
/// <remarks>
/// Never null from <see cref="IReleaseNoteService"/>: "nothing new" and "we could not ask" both
/// arrive as <see cref="Empty"/> with no notes, because a release note is decoration and a consumer
/// should not have to null-check its way around a page render.
/// </remarks>
public sealed record ReleaseNoteDeltaDto
{
    /// <summary>The version the caller asked from. Echoed back so a response can be matched to its request.</summary>
    public string SinceVersion { get; init; }

    /// <summary>The newest version in <see cref="Notes"/>, or <c>null</c> when there is nothing new.
    /// A consumer stores this as the new "last seen" once it has shown the news.</summary>
    public string LatestVersion { get; init; }

    /// <summary>The notes themselves, newest first, excluding <see cref="SinceVersion"/> itself.</summary>
    public IReadOnlyList<ReleaseNoteDto> Notes { get; init; } = [];

    /// <summary>Every note's markdown concatenated in the same order, so a caller can render one
    /// block without stitching. Empty when there is nothing new.</summary>
    public string Markdown { get; init; } = string.Empty;

    /// <summary>True when the server capped the response — the caller is further behind than one
    /// response carries, and there is older news than the oldest note here.</summary>
    public bool Truncated { get; init; }

    /// <summary>True when there is anything to show. The question a consumer actually asks.</summary>
    public bool HasNews => Notes is { Count: > 0 };

    /// <summary>Nothing new, and nothing went wrong that the caller can act on.</summary>
    public static ReleaseNoteDeltaDto Empty { get; } = new();
}
