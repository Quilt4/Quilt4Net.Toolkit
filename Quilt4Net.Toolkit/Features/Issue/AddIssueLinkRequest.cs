namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Declares a dependency from one issue to another. Requires an API key carrying the
/// <c>issue:write</c> scope.
/// </summary>
public record AddIssueLinkRequest
{
    /// <summary>The issue being pointed at, by its per-team number.</summary>
    public required int TargetNumber { get; init; }

    /// <summary>What the link asserts. See <see cref="IssueLinkKind"/>.</summary>
    public required IssueLinkKind Kind { get; init; }

    /// <summary>
    /// Why the link exists. <b>Required and non-empty</b> — an edge with no stated reason is deleted
    /// rather than drawn, because most items people assume are ordered turn out to be merely
    /// related. Stating the reason is what keeps the graph honest.
    /// </summary>
    public required string Reason { get; init; }
}
