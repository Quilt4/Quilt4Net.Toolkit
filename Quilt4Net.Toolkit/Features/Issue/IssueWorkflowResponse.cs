namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// A team's issue workflow: the states an issue may be in, and the moves between them that are
/// allowed. One workflow governs every issue on the team.
/// </summary>
public record IssueWorkflowResponse
{
    /// <summary>The defined states, in display order.</summary>
    public required IssueWorkflowStateResponse[] States { get; init; }

    /// <summary>The permitted moves. A move not listed here is rejected.</summary>
    public required IssueWorkflowTransitionResponse[] Transitions { get; init; }
}

/// <summary>One state in an <see cref="IssueWorkflowResponse"/>.</summary>
public record IssueWorkflowStateResponse
{
    /// <summary>State name, unique within the workflow, and what an issue stores.</summary>
    public required string Name { get; init; }

    /// <summary>Display order, ascending.</summary>
    public required int Order { get; init; }

    /// <summary>
    /// Whether a new issue starts here. Exactly one state is the initial one.
    /// </summary>
    public required bool IsInitial { get; init; }

    /// <summary>
    /// Whether an issue in this state counts as finished. Terminal issues are hidden from the
    /// roadmap unless they are an endpoint of a drawn edge, where they explain the edge.
    /// </summary>
    public required bool IsTerminal { get; init; }
}

/// <summary>One permitted move in an <see cref="IssueWorkflowResponse"/>.</summary>
public record IssueWorkflowTransitionResponse
{
    /// <summary>State being moved from.</summary>
    public required string From { get; init; }

    /// <summary>State being moved to.</summary>
    public required string To { get; init; }
}
