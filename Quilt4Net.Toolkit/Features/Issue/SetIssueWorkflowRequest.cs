namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Replaces a team's issue workflow. Requires an API key carrying the <c>issue:write</c> scope.
/// </summary>
/// <remarks>
/// The replacement is validated before anything is written: exactly one state must be initial, every
/// transition must name states that exist, and <b>every state currently in use by an issue must
/// still be defined</b>. A workflow that would orphan live issues is rejected and the error names
/// them, rather than leaving issues in a state the workflow no longer knows about.
/// </remarks>
public record SetIssueWorkflowRequest
{
    /// <summary>The states to define, in display order.</summary>
    public required IssueWorkflowStateResponse[] States { get; init; }

    /// <summary>The moves to permit.</summary>
    public required IssueWorkflowTransitionResponse[] Transitions { get; init; }
}
