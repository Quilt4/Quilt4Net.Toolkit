using Microsoft.Extensions.DependencyInjection;
using Tharga.Blazor.Features.BreadCrumbs;

namespace Quilt4Net.Toolkit.Blazor.Features.Content.Pages;

/// <summary>
/// Default <see cref="IPageBreadcrumbAdapter"/>. Resolves Tharga.Blazor's <c>BreadCrumbService</c>
/// optionally — when the host hasn't registered <c>AddThargaBlazor</c>, <see cref="Push"/> silently
/// no-ops so the reader still renders. That way the toolkit reader works even in hosts that don't
/// adopt Tharga.Blazor's breadcrumb system.
/// </summary>
internal sealed class TharBlazorBreadcrumbAdapter : IPageBreadcrumbAdapter
{
    private readonly BreadCrumbService _breadcrumbService;

    public TharBlazorBreadcrumbAdapter(IServiceProvider serviceProvider)
    {
        // Optional lookup: missing service is a deliberately supported configuration.
        _breadcrumbService = serviceProvider.GetService<BreadCrumbService>();
    }

    public void Push(IReadOnlyList<PageBreadcrumbSegment> chain)
    {
        if (_breadcrumbService == null || chain == null) return;
        foreach (var segment in chain)
        {
            _breadcrumbService.AddVirtualSegment(segment.Label, segment.Href);
        }
    }
}
