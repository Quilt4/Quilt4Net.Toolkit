using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

internal static class LogConfigurationGuard
{
    public const string ServiceNotRegisteredMessage =
        "Application Insights client is not registered. Call builder.AddQuilt4NetApplicationInsightsClient() in Program.cs to enable this view.";

    public static IApplicationInsightsService TryResolve(IServiceProvider services)
        => services.GetService<IApplicationInsightsService>();
}
