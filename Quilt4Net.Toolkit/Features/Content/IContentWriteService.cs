using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

/// <summary>
/// Re-assert content the server already owns — the supported way to change a key's text when
/// changing the literal in code no longer does anything.
/// </summary>
/// <remarks>
/// <para>
/// A key's stored value wins over the default a caller supplies, so that an administrator's edit
/// survives a redeploy. The gap that leaves is a value that exists and is now wrong in code: the
/// only remedies were minting a new key (<c>Heading2</c>, then <c>Heading3</c>) or hand-editing
/// every environment. This is the path that follows from the rule rather than an exception to it.
/// </para>
/// <para>
/// ⚠️ <b>Deliberately not wired into startup.</b> Nothing calls this for you, and that is the
/// design. Every instance asserting its build's strings on boot recreates the problem it solves,
/// and makes a rollback quietly revert tenant content. Call it from a reviewed maintenance action
/// or a CI step, where a person decided the text should change.
/// </para>
/// <para>
/// Requires an API key holding <c>content:write</c>, and writes land at the lowest stage — a human
/// promotes from there.
/// </para>
/// </remarks>
public interface IContentWriteService
{
    /// <summary>
    /// Write new text for a set of keys. Returns what the server did, including any language names
    /// it did not recognise.
    /// </summary>
    /// <remarks>
    /// Always check <see cref="ContentImportResult.IgnoredLanguages"/>. A name matching no
    /// configured language is skipped, not refused, so a typo otherwise returns success while the
    /// translation never appears. A CI step should fail its build on a non-empty list.
    /// </remarks>
    Task<ContentImportResult> ImportAsync(IReadOnlyCollection<ContentImportItem> items, CancellationToken cancellationToken = default);
}
