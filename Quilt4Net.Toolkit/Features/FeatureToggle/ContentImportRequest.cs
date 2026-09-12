namespace Quilt4Net.Toolkit.Features.FeatureToggle;

/// <summary>
/// Re-assert the text of content keys — the supported way to change a key whose value the server
/// already owns. Sent to <c>POST Api/Content/import</c>, which requires the <c>content:write</c>
/// scope.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> A key's stored value wins over the default a caller supplies, which is
/// correct — an administrator's edit must survive a redeploy. The gap it leaves is a value that
/// exists and is now wrong in code: changing the literal does nothing, and the workarounds are
/// minting <c>Heading2</c> or hand-editing every environment. This is the path that follows from
/// the rule rather than an exception to it (issue #161).
/// </para>
/// <para>
/// <b>Set, not reset.</b> Clearing a key so it re-seeds from code races a rolling deploy: an
/// instance still on the old build re-materializes the old value, and old and new instances coexist
/// by definition, so no ordering closes the window. An explicit write is authoritative the moment
/// it lands.
/// </para>
/// <para>
/// <b>Not for startup.</b> Call this from a reviewed maintenance action or a CI step, not from
/// application start. Every instance asserting its build's strings on boot recreates the original
/// problem with extra steps, and a rollback then quietly reverts tenant content.
/// </para>
/// </remarks>
public sealed record ContentImportRequest
{
    /// <summary>
    /// The caller's runtime environment name. It must resolve to the lowest stage, or the request
    /// is refused in full — no key in the set is written.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>The keys to write. Applied as one unit.</summary>
    public required IReadOnlyList<ContentImportItem> Items { get; init; }
}

/// <summary>One key's worth of writes within a <see cref="ContentImportRequest"/>.</summary>
public sealed record ContentImportItem
{
    /// <summary>The content key.</summary>
    public required string Key { get; init; }

    /// <summary>The application the key is stored under. Null or empty targets the shared
    /// bucket.</summary>
    /// <remarks>
    /// ⚠️ <b>Matched exactly against what is stored, not folded or normalised.</b> Naming an
    /// application the row was not stored under does not fail — it addresses a different
    /// coordinate, so the write creates a second row instead of updating the one you meant. If a
    /// value does not change after an import that reported success, this is almost always why.
    /// Read the coordinate back from the key you are changing rather than assuming it.
    /// </remarks>
    public string Application { get; init; }

    /// <summary>The instance the key is stored under. Null or empty targets rows stored without
    /// one.</summary>
    /// <remarks>Matched exactly, with the same consequence described on
    /// <see cref="Application"/>.</remarks>
    public string Instance { get; init; }

    /// <summary>How the value should be rendered. Leave null to keep what the key already
    /// has.</summary>
    public ContentFormat? ContentFormat { get; init; }

    /// <summary>
    /// New text for the default language, or null to leave the default language untouched and
    /// change only the entries in <see cref="Translations"/>.
    /// </summary>
    /// <remarks>
    /// Writing this marks every sister translation stale for review, because the text they were
    /// translated from has changed. Translations supplied in the same item clear their own flag as
    /// they land, so sending a full set does not leave the whole key flagged.
    /// </remarks>
    public string DefaultValue { get; init; }

    /// <summary>
    /// New text per language, keyed by <b>language name</b> as entered on the server, e.g.
    /// <c>{ ["Swedish"] = "Ärende" }</c>. A name matching no configured language is ignored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keyed by name rather than by language key for the same reason
    /// <see cref="GetContentRequest.Translations"/> is: a caller writing a literal in source has the
    /// language's name, not a per-tenant GUID.
    /// </para>
    /// <para>
    /// <b>A language absent here is left untouched</b>, so a Swedish correction can never blank
    /// English. That also means an unchanged value should be omitted rather than resent: this path
    /// stamps what it writes as human-authored, and rewriting an untouched machine translation
    /// would make that output eligible as a translation source for other languages.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, string> Translations { get; init; }
}
