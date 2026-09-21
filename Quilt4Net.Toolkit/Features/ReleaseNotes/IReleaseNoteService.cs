namespace Quilt4Net.Toolkit.Features.ReleaseNotes;

/// <summary>
/// Reads release notes from Quilt4Net.Server. This is the toolkit half of the feature — looking
/// notes up. Publishing them is deliberately not here: a note is written by a release pipeline over
/// plain REST or MCP, which needs no NuGet dependency.
/// </summary>
/// <remarks>
/// <para>
/// The typical use is "show the user what has changed since they were last here". The consuming
/// application owns that memory — the toolkit holds no per-user state and the server holds none
/// either. The app remembers the version it last displayed to this user, passes it to
/// <see cref="GetDeltaAsync"/>, renders what comes back, and then stores
/// <see cref="ReleaseNoteDeltaDto.LatestVersion"/> as the new mark.
/// </para>
/// <para>
/// A user with no stored version — a first-ever visit — has nothing to be caught up on, so the
/// application simply does not call. Asking anyway is not a way to get the whole history: the call
/// short-circuits here and never reaches the server.
/// </para>
/// </remarks>
public interface IReleaseNoteService
{
    /// <summary>
    /// Fetch one version's note. Returns <c>null</c> when that version has none published, when the
    /// version is not valid SemVer, or when the server cannot be reached.
    /// </summary>
    /// <param name="version">The version to read, as SemVer 2.0 (for example <c>1.4.0</c>).</param>
    /// <param name="application">Optional application override; <c>null</c> uses the toolkit's
    /// configured application name.</param>
    /// <param name="cancellationToken">Cancels the call. The configured HTTP timeout still applies.</param>
    Task<ReleaseNoteDto> GetAsync(string version, string application = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch every note published after <paramref name="sinceVersion"/>, newest first. Never null —
    /// "nothing new", "no version to ask from" and "the server did not answer" all come back as
    /// <see cref="ReleaseNoteDeltaDto.Empty"/>.
    /// </summary>
    /// <param name="sinceVersion">The version the caller has already shown this user. Required: with
    /// nothing, or with something that is not SemVer, no request is made.</param>
    /// <param name="includePreRelease">Include pre-release versions such as <c>2.0.0-beta.1</c>.
    /// Off by default — most end users should not be told about a beta.</param>
    /// <param name="application">Optional application override; <c>null</c> uses the toolkit's
    /// configured application name.</param>
    /// <param name="cancellationToken">Cancels the call. The configured HTTP timeout still applies.</param>
    Task<ReleaseNoteDeltaDto> GetDeltaAsync(string sinceVersion, bool includePreRelease = false, string application = null, CancellationToken cancellationToken = default);
}
