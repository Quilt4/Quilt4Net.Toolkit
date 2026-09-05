namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// One reference from an issue to where it came from, or to the ticket it <i>is</i>.
/// </summary>
public record IssueReferenceResponse
{
    /// <summary>
    /// The source system, lower-cased — <c>github</c>, <c>backlog</c>, <c>code</c>, <c>jira</c>,
    /// <c>trello</c>, <c>session</c>.
    /// </summary>
    /// <remarks>
    /// <b>An open string, not an enum</b>, unlike <see cref="IssueLinkKind"/>, <see cref="RoadmapBand"/>
    /// and <see cref="IssueEffort"/> — those are closed because the roadmap format defines them.
    /// Source kinds are an extensibility point, and an enum here would force a Toolkit release every
    /// time a new tracker appeared.
    /// </remarks>
    public required string Kind { get; init; }

    /// <summary>
    /// What the reference points at within that system — <c>Toolkit#172</c>, <c>backlog.md</c>,
    /// <c>LanguageController.cs:52</c>. Carries the meaning: <see cref="Url"/> is a convenience.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>Whether this is a stale-tolerant hint or a key. See <see cref="IssueReferenceRole"/>.</summary>
    public required IssueReferenceRole Role { get; init; }

    /// <summary>
    /// A link to the reference, or empty when it has none. <b>Optional by design</b> — a code comment
    /// and a backlog row genuinely have no URL, so requiring one would push callers into inventing
    /// them.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>Display text, or empty to render <see cref="Value"/> instead.</summary>
    public required string Label { get; init; }
}
