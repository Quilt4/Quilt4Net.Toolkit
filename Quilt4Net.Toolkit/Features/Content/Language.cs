namespace Quilt4Net.Toolkit.Features.Content;

public record Language
{
    public static readonly Guid DeveloperLanguageKey = Guid.Parse("8C12E829-318E-40DA-86E9-6B37A68EFFD1");
    public static readonly Guid NoApiKeyLanguageKey = Guid.Parse("00000000-0000-0000-0000-000000000001");

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
