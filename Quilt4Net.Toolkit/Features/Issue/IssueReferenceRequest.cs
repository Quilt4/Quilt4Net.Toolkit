namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// One reference to write onto an issue. See <see cref="IssueReferenceResponse"/> for what the
/// fields mean.
/// </summary>
/// <remarks>
/// References travel on <see cref="CreateIssueRequest"/> and <see cref="UpdateIssueRequest"/> rather
/// than through add/remove endpoints of their own, which is where <see cref="AddIssueLinkRequest"/>
/// goes. A link is an edge between two issues and needs the server to check it — a reason, a missing
/// target, a cycle. A reference is a property of one issue with nothing to check against, so the
/// endpoints that already replace an issue's fields are enough.
/// </remarks>
public record IssueReferenceRequest
{
    /// <summary>
    /// The source system. Lower-cased and whitespace-collapsed by the server, so <c>GitHub</c> and
    /// <c>github</c> are the same kind. Required.
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>What the reference points at within that system. Required.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// Whether this is a hint or a key. Defaults to <see cref="IssueReferenceRole.Provenance"/>,
    /// which is the safe default: it claims nothing, whereas a wrongly-asserted
    /// <see cref="IssueReferenceRole.Identity"/> would block a later issue from claiming the ticket
    /// it really is.
    /// </summary>
    public IssueReferenceRole Role { get; init; } = IssueReferenceRole.Provenance;

    /// <summary>A link to the reference. Optional.</summary>
    public string Url { get; init; }

    /// <summary>Display text. Optional — <see cref="Value"/> is shown when this is empty.</summary>
    public string Label { get; init; }
}
