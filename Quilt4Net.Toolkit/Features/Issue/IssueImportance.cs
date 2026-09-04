using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Issue;

/// <summary>
/// How much an issue matters, paired with <see cref="IssueEffort"/> to give the house ordering rule:
/// importance first, then effort ascending — highest impact for least work.
/// </summary>
/// <remarks>
/// The three values are the backlog's own vocabulary rather than a new scale, so an issue and a
/// backlog row can be compared without translating between them.
/// <para>
/// Importance is deliberately <b>nullable</b> on an issue. An ungraded issue means nobody has decided
/// yet, which is worth seeing; defaulting it to <see cref="Nice"/> would assert a judgement no one
/// made and hide the ones still needing triage.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueImportance
{
    /// <summary>Something is broken or blocked now, and it outranks everything else.</summary>
    Critical,

    /// <summary>Worth doing, and worth planning around.</summary>
    Important,

    /// <summary>Wanted, but nothing suffers while it waits.</summary>
    Nice
}
