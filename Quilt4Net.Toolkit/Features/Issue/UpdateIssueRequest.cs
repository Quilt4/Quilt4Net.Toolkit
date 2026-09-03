namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Replaces an issue's editable fields. Requires an API key carrying the <c>issue:write</c> scope.
/// </summary>
/// <remarks>
/// <b>This is a replace, not a merge.</b> Every field is written as supplied, so an omitted optional
/// field is <i>cleared</i> rather than left alone — read the issue, change what you mean to change,
/// and send the whole thing back.
/// <para>
/// The alternative — treating <c>null</c> as "leave unchanged" — reads as friendlier but leaves no
/// way to clear a field, and makes every request ambiguous about which of the two it meant. Replace
/// semantics are what the HTTP verb already promises.
/// </para>
/// <para>
/// <see cref="IssueResponse.State"/> is deliberately absent: a state change has to be checked
/// against the team's workflow, so it goes through the state endpoint instead. The MCP
/// <c>quilt4net.issue.update</c> tool wraps this in a read-modify-write, so an agent there may send
/// only the fields it wants to change.
/// </para>
/// </remarks>
public record UpdateIssueRequest
{
    /// <summary>Short one-line summary. Required.</summary>
    public required string Title { get; init; }

    /// <summary>Body text. Cleared when omitted.</summary>
    public string Content { get; init; }

    /// <summary>Route (roadmap lane). Cleared when omitted, which takes the issue off the map.</summary>
    public string Route { get; init; }

    /// <summary>Order-axis band. Defaults to <see cref="RoadmapBand.Later"/>.</summary>
    public RoadmapBand Band { get; init; } = RoadmapBand.Later;

    /// <summary>Assigned team member key. Cleared when omitted, which unassigns the issue.</summary>
    public string AssignedUserKey { get; init; }

    /// <summary>Rough size. Cleared when omitted.</summary>
    public IssueEffort? Effort { get; init; }
}
