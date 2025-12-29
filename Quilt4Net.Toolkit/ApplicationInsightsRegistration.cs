using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApplicationInsightsClient(this IServiceCollection services, Action<ApplicationInsightsOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

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