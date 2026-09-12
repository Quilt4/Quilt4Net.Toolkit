namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// Push a set of content pages. Sent to <c>POST Api/ContentPage</c>, which requires the
/// <c>content:write</c> scope.
/// </summary>
/// <remarks>
/// <para>
/// <b>Always a set, never a single page.</b> A one-page push is a set of one, so there is no
/// separate bulk endpoint to keep in step with this one. The consumer this was built for
/// (issue #192) pushes a 20-60 page manual across two languages on every release; one call per
/// page per language is a round trip per page for something a pipeline does every deploy.
/// </para>
/// <para>
/// <b>Writes land at the lowest stage, always.</b> The server refuses anything else, and moving a
/// page up is an explicit promotion. That is what lets a repository own the authored source while
/// an operator still edits and promotes without a deploy: CI owns the bottom stage, a human owns
/// everything above it, and the two never contend for the same row.
/// </para>
/// <para>
/// <b>Idempotent.</b> Re-sending an unchanged payload leaves the stored pages byte-identical, so a
/// pipeline can push on every deploy without churning content. See
/// <see cref="ContentSectionWrite.Id"/> for how that holds even when the caller supplies no ids.
/// </para>
/// </remarks>
public sealed record ContentPageUpsertRequest
{
    /// <summary>
    /// The caller's runtime environment name, resolved server-side to a stage. It must resolve to
    /// the lowest stage or the whole request is refused — no page in the set is written.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// The pages to write. Order is irrelevant: a page may name a parent that appears later in the
    /// list, or that already exists on the server. See <see cref="ContentPageWrite.ParentSlug"/>.
    /// </summary>
    public required IReadOnlyList<ContentPageWrite> Pages { get; init; }
}

/// <summary>
/// One page in a <see cref="ContentPageUpsertRequest"/>, identified by
/// <see cref="Slug"/> + <see cref="LanguageKey"/> at the lowest stage.
/// </summary>
/// <remarks>
/// This is the <b>authored</b> shape, not the read shape. <see cref="ContentPageDto"/> carries
/// rendered HTML for its text elements because a renderer wants HTML; this carries the Markdown
/// source, because that is what a repository holds and what an editor edits. Round-tripping a
/// <see cref="ContentPageDto"/> back into a write would silently replace every author's Markdown
/// with the HTML it happened to render to.
/// </remarks>
public sealed record ContentPageWrite
{
    /// <summary>
    /// URL-safe identifier within the team, unique per (stage, language). May contain forward
    /// slashes for path-style hierarchies.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>The default language uses <see cref="Guid.Empty"/>; a translation carries the
    /// language's own key.</summary>
    public Guid LanguageKey { get; init; }

    /// <summary>Human-readable title — the menu label and the page heading.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// The parent page's <b>slug</b>, or null for a root page.
    /// </summary>
    /// <remarks>
    /// Deliberately a slug and not a database id. A repository knows that <c>guides/registrera</c>
    /// belongs under <c>guides</c>; it does not know the parent's <c>ObjectId</c>, and looking one
    /// up before every write costs a read per page and makes the push order-dependent. The server
    /// resolves parents across the whole request first, so a parent may appear anywhere in
    /// <see cref="ContentPageUpsertRequest.Pages"/> — or only on the server, already written by an
    /// earlier push.
    /// </remarks>
    public string ParentSlug { get; init; }

    /// <summary>Order among siblings. Sorted ascending; gaps are fine, so inserting a page does not
    /// mean renumbering its siblings.</summary>
    public int Order { get; init; }

    /// <summary>Whether the page appears in the navigation menu. A page can be reachable by direct
    /// slug without being listed.</summary>
    public bool ShowInMenu { get; init; }

    /// <summary>The page body. An empty list is valid — a page with only a title is a usable tree
    /// placeholder.</summary>
    public required IReadOnlyList<ContentSectionWrite> Sections { get; init; }
}
