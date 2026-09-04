namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// One outgoing dependency from the issue that carries it, to the issue named by
/// <see cref="TargetNumber"/>.
/// </summary>
public record IssueLinkResponse
{
    /// <summary>The issue this link points at, by its per-team number.</summary>
    public required int TargetNumber { get; init; }

    /// <summary>What the link asserts. See <see cref="IssueLinkKind"/>.</summary>
    public required IssueLinkKind Kind { get; init; }

    /// <summary>
    /// Why the link exists. Always populated — the server rejects a link with an empty reason,
    /// because an edge with no stated reason is deleted rather than drawn.
    /// </summary>
    public required string Reason { get; init; }
}
