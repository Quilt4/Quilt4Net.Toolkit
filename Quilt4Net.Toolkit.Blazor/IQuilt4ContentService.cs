namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Simplified content service that handles language selection automatically.
/// </summary>
public interface IQuilt4ContentService
{
    /// <summary>
    /// Get content by key using the currently selected language.
    /// </summary>
    /// <param name="key">Content key.</param>
    /// <param name="defaultValue">Value to return if the server is unreachable or the key is unknown.</param>
    /// <param name="application">
    /// Application scope. Convention:
    /// <list type="bullet">
    /// <item><c>null</c> — default: resolve the current application name (ContentOptions.Application or the entry assembly name).</item>
    /// <item><c>""</c> — shared (cross-application).</item>
    /// <item>any other value — that exact application.</item>
    /// </list>
    /// </param>
    Task<string> GetAsync(string key, string defaultValue, string application = null);

    /// <summary>
    /// Get content by key using the currently selected language, with an authoritative default text
    /// declared per language (issue #135) — for domain-critical vocabulary whose wording must be
    /// correct before or without a stored/translated value (e.g. legal/archival terms).
    /// </summary>
    /// <param name="key">Content key.</param>
    /// <param name="defaultsByLanguage">
    /// Default text keyed by two-letter ISO language code (e.g. <c>"en"</c>, <c>"sv"</c>), matched
    /// case-insensitively. The default used is chosen by this chain: code default for the active UI
    /// culture → English (<c>"en"</c>) code default → the content key. A stored translation for the
    /// selected language still takes precedence over the resolved default.
    /// </param>
    /// <param name="application">Application scope; see <see cref="GetAsync(string, string, string)"/>.</param>
    Task<string> GetAsync(string key, IReadOnlyDictionary<string, string> defaultsByLanguage, string application = null);

    /// <summary>
    /// Get content by key using the currently selected language, supplying the default-language value
    /// AND exact direct translations for one or more other languages (issue #135 server half). Each
    /// supplied translation is stored verbatim on the server as authoritative for the matching
    /// language when the key is first materialized, and AI translation is <b>skipped</b> for those
    /// languages; the remaining AI-enabled languages are machine-translated as usual.
    /// </summary>
    /// <param name="key">Content key.</param>
    /// <param name="defaultValue">The default-language value (as in <see cref="GetAsync(string, string, string)"/>).</param>
    /// <param name="translations">
    /// Exact values keyed by <b>language name</b> exactly as entered on the server (e.g.
    /// <c>{ ["Swedish"] = "Ärende" }</c>). A name matching no configured language is ignored. Pass
    /// null/empty for "default only" — equivalent to the two-argument overload.
    /// </param>
    /// <param name="application">Application scope; see <see cref="GetAsync(string, string, string)"/>.</param>
    Task<string> GetAsync(string key, string defaultValue, IReadOnlyDictionary<string, string> translations, string application = null);
}
