namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// One key in a bulk content read — the same information a single-key <c>GetAsync</c> takes, so a
/// key the server has not seen still materializes with the text its caller declared.
/// </summary>
public sealed record ContentRequest
{
    public required string Key { get; init; }

    /// <summary>Text to use if nothing is stored, and to seed the key with if it does not exist.</summary>
    public required string DefaultValue { get; init; }

    /// <summary>
    /// Optional exact translations keyed by <b>language name</b>, e.g.
    /// <c>{ ["Swedish"] = "Ärende" }</c>. Stored verbatim as authoritative when the key is first
    /// materialized, and those languages are skipped by AI translation. A name matching no
    /// configured language is ignored.
    /// </summary>
    public IReadOnlyDictionary<string, string> Translations { get; init; }
}
