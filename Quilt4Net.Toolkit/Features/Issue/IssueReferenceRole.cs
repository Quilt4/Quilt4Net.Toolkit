using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// What a reference is <i>for</i>. Two jobs hide inside "where did this issue come from", and they
/// have opposite tolerances for going stale, so the field says which one is meant rather than
/// leaving every reader to guess from the <see cref="IssueReferenceResponse.Kind"/>.
/// </summary>
/// <remarks>
/// Closed, unlike <see cref="IssueReferenceResponse.Kind"/>. The set of source systems is an
/// extensibility point and must stay open; the set of jobs a reference can do is not — there are
/// exactly two, and a third would be a different feature rather than a new tracker.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueReferenceRole
{
    /// <summary>
    /// A human hint about where the issue came from — a backlog row, a code comment, a conversation.
    /// <b>Allowed to go stale and never load-bearing:</b> <c>LanguageController.cs:52</c> is wrong
    /// after the next edit, and that is tolerable.
    /// <para>
    /// Provenance references are deliberately <i>not</i> unique. Many issues legitimately share one
    /// origin — 68 of this tracker's first 78 issues came off the same backlog file — so requiring
    /// uniqueness would reject the very data the field exists to hold.
    /// </para>
    /// </summary>
    Provenance,

    /// <summary>
    /// This issue <b>is</b> that ticket: a GitHub issue, a Jira key, a Trello card. A key, not a
    /// hint.
    /// <para>
    /// The server refuses to let two issues claim the same identity, and that refusal is the whole
    /// point of the role: it is what lets a repeated import recognise a candidate it has already
    /// tracked and update it, instead of creating a duplicate.
    /// </para>
    /// </summary>
    Identity
}
