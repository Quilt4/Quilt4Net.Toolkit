using Microsoft.AspNetCore.Mvc.Abstractions;
using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Health.Framework;

internal class CustomRouteDescriptorProvider : IActionDescriptorProvider
{
    private readonly Quilt4NetHealthApiOptions _apiOptions;

    public CustomRouteDescriptorProvider(Quilt4NetHealthApiOptions apiOptions)
    {
        _apiOptions = apiOptions;
    }

    public int Order => -1000; // Ensure it runs early

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var descriptor in context.Results)
        {
            if (descriptor.RouteValues.TryGetValue("controller", out var item))
            {
                if (item == "Health")
                {
                    descriptor.RouteValues["controller"] = _apiOptions.ControllerName;
                }
            }
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context) { }
}