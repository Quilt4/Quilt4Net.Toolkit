namespace Quilt4Net.Toolkit.Features.Content;

public record Language
{
    public static readonly Guid DeveloperLanguageKey = Guid.Parse("8C12E829-318E-40DA-86E9-6B37A68EFFD1");
    public static readonly Guid NoApiKeyLanguageKey = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// The second developer-only pseudo-language: every key resolves to its own key name instead of
    /// its text, so a screen can be read as a map of the keys behind it. Sibling of
    /// <see cref="DeveloperLanguageKey"/> — that one answers "is this managed content?", this one
    /// answers "*which* key is it?", the question you have when you want to change the text.
    /// </summary>
    /// <remarks>
    /// Fixed rather than generated: the selected language key is persisted in the host's local
    /// storage, so a value that differed per process would not survive a restart.
    /// </remarks>
    public static readonly Guid KeyLanguageKey = Guid.Parse("3B7F6A54-9C41-4E28-9A5D-2F8E0D6C7B13");

    /// <summary>
    /// Whether <paramref name="languageKey"/> is one of the synthetic languages the client invents
    /// rather than one the server serves content for. Every remote path — cache warm-up included —
    /// has to skip these, and centralising the test is what keeps a newly added pseudo-language from
    /// slipping past one guard and generating traffic for content that cannot exist.
    /// </summary>
    public static bool IsPseudo(Guid languageKey)
        => languageKey == DeveloperLanguageKey || languageKey == KeyLanguageKey || languageKey == NoApiKeyLanguageKey;

    public Guid Key { get; set; }
    public string Name { get; set; }
    public bool Developer { get; set; }

    /// <summary>
    /// Stable ISO-639 code for this language — <c>"sv"</c>, <c>"en"</c>, <c>"es"</c> — or
    /// <c>null</c> when the server has not been given one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identifier a host can map <b>its own</b> language identity onto. <see cref="Key"/> is
    /// per-tenant, so hardcoding it does not survive a move between teams or environments, and
    /// <see cref="Name"/> is a display string that spelling, localisation or a rename can change
    /// underneath a match. Neither is safe to key off; this is (issue #144).
    /// </para>
    /// <para>
    /// <b>Nullable on purpose, and expect nulls.</b> A server older than this field never sends one,
    /// and a language whose code could not be determined keeps it null rather than being given a
    /// guess — a wrong code silently routes a host to the wrong language, which is the exact bug
    /// this field exists to prevent. Treat null as "cannot be matched by code", not as an error.
    /// </para>
    /// </remarks>
    public string Code { get; set; }
}
