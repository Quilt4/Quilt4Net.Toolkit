namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// Who says they are working on an issue, and from which machine.
/// </summary>
/// <remarks>
/// <para>
/// Metadata the caller stated, not an identity the server checked. A team API key carries no user,
/// so the server records whatever the <c>X-Quilt4Net-User</c> and <c>X-Quilt4Net-Machine</c> headers
/// (or explicit arguments) said. That is enough for what a claim is for — seeing that another session
/// already has the issue in hand — and it is why taking a claim somebody else holds warns rather than
/// refuses.
/// </para>
/// <para>
/// The same user on two machines is two claimants. The collision this exists to show is two sessions
/// working one issue, and that happens far more often on one person's two machines than between two
/// people.
/// </para>
/// </remarks>
public record IssueClaimResponse
{
    /// <summary>The stated user. Empty when only a machine was stated.</summary>
    public required string User { get; init; }

    /// <summary>The stated machine. Empty for a claim taken from a browser, which has none to name.</summary>
    public required string Machine { get; init; }

    /// <summary>When this claimant took the issue (UTC). Kept when the same claimant claims again.</summary>
    public required DateTime ClaimedUtc { get; init; }

    /// <summary>The claimant's last write to the issue (UTC). Every transition, claim or update by the holder moves it.</summary>
    public required DateTime LastSeenUtc { get; init; }

    /// <summary>
    /// Whether the holder has gone quiet long enough that the claim should not be trusted.
    /// </summary>
    /// <remarks>
    /// Judged by the server, so every client applies the same threshold rather than each choosing
    /// its own. A session that dies mid-issue leaves its claim behind and nothing else would ever
    /// clear it; a stale claim is drawn faded so a reader can tell "being worked on" from "was".
    /// </remarks>
    public required bool IsStale { get; init; }
}
