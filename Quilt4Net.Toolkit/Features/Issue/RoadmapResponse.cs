namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// The roadmap, already laid out. This is the shape the view component draws.
/// </summary>
/// <remarks>
/// The projection is built on the server so the component does not have to re-derive lanes and bands
/// from a flat issue list — two consumers deriving the same layout is two places for it to differ.
/// </remarks>
public record RoadmapResponse
{
    /// <summary>The lanes, in display order. Each is one route.</summary>
    public required RoadmapRouteResponse[] Routes { get; init; }

    /// <summary>
    /// Every edge worth drawing, already filtered to links whose endpoints both appear in
    /// <see cref="Routes"/>.
    /// </summary>
    public required RoadmapEdgeResponse[] Edges { get; init; }

    /// <summary>
    /// Issues that carry no route at all, and so belong in no lane.
    /// </summary>
    public required int UnroutedCount { get; init; }

    /// <summary>
    /// Issues that <b>do</b> carry a route but are still not drawn — their route fell outside the
    /// lane cap, or they are finished and explain no edge.
    /// </summary>
    /// <remarks>
    /// Counted separately from <see cref="UnroutedCount"/> because the two have different fixes and
    /// conflating them produces a false statement. The first version reported both as "carry no
    /// route", which was wrong the first time a real import exceeded the lane cap: three parked
    /// issues with a perfectly good route were described as having none.
    /// </remarks>
    public required int HiddenCount { get; init; }

    /// <summary>When this projection was built (UTC).</summary>
    public required DateTime GeneratedUtc { get; init; }
}

/// <summary>One lane of the roadmap.</summary>
public record RoadmapRouteResponse
{
    /// <summary>Route name, as carried by the issues in it.</summary>
    public required string Name { get; init; }

    /// <summary>Items in the <see cref="RoadmapBand.Now"/> band.</summary>
    public required RoadmapItemResponse[] Now { get; init; }

    /// <summary>Items in the <see cref="RoadmapBand.Next"/> band.</summary>
    public required RoadmapItemResponse[] Next { get; init; }

    /// <summary>Items in the <see cref="RoadmapBand.Later"/> band.</summary>
    public required RoadmapItemResponse[] Later { get; init; }
}

/// <summary>One issue as it appears on the map.</summary>
public record RoadmapItemResponse
{
    /// <summary>The issue's per-team number.</summary>
    public required int Number { get; init; }

    /// <summary>Short one-line summary.</summary>
    public required string Title { get; init; }

    /// <summary>Current workflow state.</summary>
    public required string State { get; init; }

    /// <summary>Assigned team member key, or empty when unassigned.</summary>
    public required string AssignedUserKey { get; init; }

    /// <summary>Rough size, rendered on the item as <c>· S</c>, <c>· M</c> or <c>· L</c>.</summary>
    public required IssueEffort? Effort { get; init; }

    /// <summary>
    /// Whether the issue is in a terminal state. A terminal item is on the map only because it
    /// explains an edge, and is drawn as context rather than as work.
    /// </summary>
    public required bool IsTerminal { get; init; }

    /// <summary>
    /// Whether this is a quick win — small, and with nothing pointing at it. These are what someone
    /// picks up with an hour free, and they are invisible in any list sorted by importance.
    /// </summary>
    public required bool IsQuickWin { get; init; }
}

/// <summary>One drawn dependency between two items on the map.</summary>
public record RoadmapEdgeResponse
{
    /// <summary>Number of the issue the edge leaves.</summary>
    public required int FromNumber { get; init; }

    /// <summary>Number of the issue the edge points at.</summary>
    public required int ToNumber { get; init; }

    /// <summary>Which of the three edge kinds this is, and so how it is drawn.</summary>
    public required IssueLinkKind Kind { get; init; }

    /// <summary>Why the edge exists. Always present, and always rendered.</summary>
    public required string Reason { get; init; }
}
