using System.Globalization;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Resolves the authoritative <em>default</em> text for a content key when a per-language default
/// set is supplied (issue #135). Domain-critical vocabulary (e.g. Swedish legal/archival terms) can
/// declare an authoritative default per language so the correct wording renders before — or without —
/// any stored/translated value, bypassing lossy machine translation.
///
/// The caller passes the resolved value as the <c>defaultValue</c> to
/// <c>IContentService.GetContentAsync</c>, so a stored translation (when present) still takes
/// precedence. The default itself is chosen with this fallback chain:
/// <list type="number">
/// <item>code default for the active UI culture (<see cref="CultureInfo.CurrentUICulture"/>, two-letter ISO code);</item>
/// <item>English code default (<c>"en"</c> in the set, or the single invariant default);</item>
/// <item>the content key.</item>
/// </list>
/// Language-code lookups are case-insensitive.
/// </summary>
internal static class LanguageDefaultResolver
{
    public static string Resolve(IReadOnlyDictionary<string, string> defaultsByLanguage, string invariantDefault, string key)
        => Resolve(defaultsByLanguage, invariantDefault, key, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public static string Resolve(IReadOnlyDictionary<string, string> defaultsByLanguage, string invariantDefault, string key, string activeCultureCode)
    {
        if (defaultsByLanguage is { Count: > 0 })
        {
            if (TryGet(defaultsByLanguage, activeCultureCode, out var forCulture)) return forCulture;
            if (TryGet(defaultsByLanguage, "en", out var forEnglish)) return forEnglish;
        }

        // The single Default is treated as the English/default-language default (backward compatible).
        if (!string.IsNullOrEmpty(invariantDefault)) return invariantDefault;

        return key;
    }

    private static bool TryGet(IReadOnlyDictionary<string, string> defaults, string code, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(code)) return false;

        foreach (var kv in defaults)
        {
            if (string.Equals(kv.Key, code, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(kv.Value))
            {
                value = kv.Value;
                return true;
            }
        }

        return false;
    }
}
