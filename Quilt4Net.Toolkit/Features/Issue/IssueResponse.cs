namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// One issue on a team's tracker.
/// </summary>
public record IssueResponse
{
    /// <summary>
    /// Per-team sequential number, and the issue's stable human reference (<c>#12</c>). Assigned by
    /// the server on creation and never reused, including after a delete.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>Short one-line summary.</summary>
    public required string Title { get; init; }

    /// <summary>Body text. May be empty.</summary>
    public required string Content { get; init; }

    /// <summary>
    /// The named route this issue belongs to — the roadmap's lane. Empty when the issue belongs to
    /// no route, in which case it stays off the map.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>Where the issue sits on the roadmap's order axis.</summary>
    public required RoadmapBand Band { get; init; }

    /// <summary>
    /// Current state, naming one of the states in the team's workflow. Changed through the
    /// state endpoint rather than a general update, so the transition can be validated.
    /// </summary>
    public required string State { get; init; }

    /// <summary>Key of the assigned team member, or empty when unassigned.</summary>
    public required string AssignedUserKey { get; init; }

    /// <summary>Rough size, or <c>null</c> when unsized.</summary>
    public required IssueEffort? Effort { get; init; }

    /// <summary>
    /// Dependencies declared <b>from</b> this issue. An issue does not carry its inbound links;
    /// read the whole set, or the roadmap projection, to see those.
    /// </summary>
    public required IssueLinkResponse[] Links { get; init; }

    /// <summary>When the issue was created (UTC).</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>When the issue was last changed (UTC).</summary>
    public required DateTime UpdatedUtc { get; init; }
}
