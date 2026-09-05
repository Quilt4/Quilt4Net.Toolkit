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
    /// How much this matters, or <c>null</c> when nobody has graded it yet. Paired with
    /// <see cref="Effort"/> for the importance-then-effort ordering.
    /// </summary>
    public required IssueImportance? Importance { get; init; }

    /// <summary>
    /// Dependencies declared <b>from</b> this issue. An issue does not carry its inbound links;
    /// read the whole set, or the roadmap projection, to see those.
    /// </summary>
    public required IssueLinkResponse[] Links { get; init; }

    /// <summary>
    /// Where this issue came from, and the tickets it is. Empty when nothing was recorded.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> <c>required</c>, unlike every other property here. The Toolkit
    /// publishes before the Server that populates it — the csproj comment in <c>Quilt4Net.Server</c>
    /// spells out why that order cannot be reversed — so for the length of that window a client on
    /// this version talks to a server that sends no <c>references</c> at all. A required member would
    /// turn that window into a deserialization failure on every read; defaulting to empty makes it a
    /// non-event.
    /// </remarks>
    public IssueReferenceResponse[] References { get; init; } = [];

    /// <summary>When the issue was created (UTC).</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>When the issue was last changed (UTC).</summary>
    public required DateTime UpdatedUtc { get; init; }
}
