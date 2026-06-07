namespace Quilt4Net.Toolkit.Blazor.Features.Content.Pages;

/// <summary>
/// Adapter the reader uses to push a page's ancestor chain into whatever breadcrumb system the host
/// app uses. Kept abstract so the toolkit doesn't hard-depend on Tharga.Blazor's
/// <c>BreadCrumbService</c> — the default implementation forwards to it via optional DI lookup, so
/// hosts that don't register Tharga.Blazor (e.g. tests) get a silent no-op.
/// </summary>
public interface IPageBreadcrumbAdapter
{
    /// <summary>Push each ancestor in root → parent order, then the page itself, as virtual
    /// breadcrumb segments. Each call is independent; the adapter does not deduplicate or remove
    /// previous segments.</summary>
    void Push(IReadOnlyList<PageBreadcrumbSegment> chain);
}

public readonly record struct PageBreadcrumbSegment(string Label, string Href);
