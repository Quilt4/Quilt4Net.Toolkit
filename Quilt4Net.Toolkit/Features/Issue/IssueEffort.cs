namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Rough size of an issue. Effort is the one field a roadmap restates from the underlying record,
/// because quick wins — small items with nothing pointing at them — are invisible without it.
/// </summary>
public enum IssueEffort
{
    /// <summary>Small — the kind of thing someone clears in an hour.</summary>
    S,

    /// <summary>Medium.</summary>
    M,

    /// <summary>Large. A route whose entry point is <see cref="L"/> is a route nobody starts.</summary>
    L
}
