using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Quilt4Net.Toolkit.Api.Framework;

internal class CustomRouteDescriptorProvider : IActionDescriptorProvider
{
    private readonly Quilt4NetApiOptions _options;

    public CustomRouteDescriptorProvider(Quilt4NetApiOptions options)
    {
        _options = options;
    }

    public int Order => -1000; // Ensure it runs early

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var descriptor in context.Results)
        {
            if (descriptor.RouteValues["controller"] == "Health")
            {
                descriptor.RouteValues["controller"] = _options.ControllerName;
            }
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context) { }
}