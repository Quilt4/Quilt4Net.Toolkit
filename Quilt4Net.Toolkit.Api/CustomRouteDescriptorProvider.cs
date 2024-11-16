using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Quilt4Net.Toolkit.Api;

internal class CustomRouteDescriptorProvider : IActionDescriptorProvider
{
    public int Order => -1000; // Ensure it runs early

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var descriptor in context.Results)
        {
            if (descriptor.RouteValues["controller"] == "Health")
            {
                descriptor.RouteValues["controller"] = Quilt4NetRegistration.Options.ControllerName; // Replace route value
            }
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context) { }
}