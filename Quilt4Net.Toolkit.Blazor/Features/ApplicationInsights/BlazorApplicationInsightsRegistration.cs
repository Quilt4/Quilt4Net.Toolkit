using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Blazor.Framework;

namespace Quilt4Net.Toolkit.Blazor.Features.ApplicationInsights;

public static class BlazorApplicationInsightsRegistration
{
    /// <summary>
    /// Register the Blazor-side wiring for remote Application Insights configuration:
    /// the toolkit-level remote provider plus the circuit-scoped selector that powers
    /// the in-component dropdown when the team has more than one configuration. Use this
    /// instead of <see cref="ApplicationInsightsRegistration.AddQuilt4NetApplicationInsightsClientRemote(IHostApplicationBuilder, Action{RemoteConfigurationOptions})"/>
    /// in Blazor host applications.
    /// </summary>
    public static IServiceCollection AddQuilt4NetBlazorApplicationInsightsClientRemote(
        this IHostApplicationBuilder builder,
        Action<RemoteConfigurationOptions> options = null)
    {
        return builder.Services.AddQuilt4NetBlazorApplicationInsightsClientRemote(builder.Configuration, options);
    }

    public static IServiceCollection AddQuilt4NetBlazorApplicationInsightsClientRemote(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RemoteConfigurationOptions> options = null)
    {
        services.AddQuilt4NetApplicationInsightsClientRemote(configuration, options);
        services.AddBlazoredLocalStorage();
        services.AddScoped<IApplicationInsightsConfigurationSelector, ApplicationInsightsConfigurationSelector>();
        // MetricsView / MetricChart consume this to render chart axis labels in the browser's
        // local timezone. TryAdd so it's idempotent — also registered from AddQuilt4NetBlazorContent
        // so most consumers get it from one path or the other.
        services.TryAddScoped<IBrowserTimeZoneAccessor, BrowserTimeZoneAccessor>();
        return services;
    }
}
