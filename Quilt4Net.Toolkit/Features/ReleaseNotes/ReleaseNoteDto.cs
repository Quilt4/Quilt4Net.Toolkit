namespace Quilt4Net.Toolkit.Features.ReleaseNotes;

/// <summary>
/// One application version's release note, as it travels between Quilt4Net.Server and a consumer.
/// The body is markdown: the server stores it verbatim and never renders it, so the consuming app
/// decides how (and whether) to display it.
/// </summary>
public sealed record ReleaseNoteDto
{
    /// <summary>The version this note describes, spelled as it was published (SemVer 2.0).</summary>
    public string Version { get; init; }

    /// <summary>True for a pre-release version. The delta leaves these out unless asked for them.</summary>
    public bool IsPreRelease { get; init; }

    /// <summary>When the note was last written. Republishing a version moves this.</summary>
    public DateTime PublishedUtc { get; init; }

    /// <summary>The note itself, in markdown.</summary>
    public string Markdown { get; init; }
}
