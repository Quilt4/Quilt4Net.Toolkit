using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.Health;
using Tharga.Cache;
using Tharga.Cache.Persist;

namespace Quilt4Net.Toolkit;

public static class ApplicationInsightsRegistration
{
    /// <summary>
    /// Register client for reading Application Insights data.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApplicationInsightsClient(this IHostApplicationBuilder builder, Action<ApplicationInsightsOptions> options = null)
    {
        builder.Services.AddQuilt4NetApplicationInsightsClient(builder.Configuration, options);
    }

    /// <summary>
    /// Register client for reading Application Insights data.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApplicationInsightsClient(this IServiceCollection services, IConfiguration configuration, Action<ApplicationInsightsOptions> options = null)
    {
        var o = configuration?.GetSection("Quilt4Net:ApplicationInsights").Get<ApplicationInsightsOptions>() ?? new ApplicationInsightsOptions();

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddTransient<IApplicationInsightsService, ApplicationInsightsService>();
        services.AddTransient<IHealthClient, HealthClient>();

        services.AddCache(s =>
        {
            s.RegisterType<EnvironmentOption[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromHours(1);
            });
        });
    }
}