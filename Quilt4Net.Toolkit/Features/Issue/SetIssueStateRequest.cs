namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Moves an issue to another state. Requires an API key carrying the <c>issue:write</c> scope.
/// </summary>
/// <remarks>
/// The move is checked against the team's workflow. A transition the workflow does not permit is
/// rejected, and the rejection names the states the issue <i>can</i> reach from where it is.
/// </remarks>
public record SetIssueStateRequest
{
    /// <summary>The state to move to. Must name a state defined by the team's workflow.</summary>
    public required string State { get; init; }
}
