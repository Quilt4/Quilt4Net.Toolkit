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
            // Environment list rarely changes — hold it for an hour.
            s.RegisterType<EnvironmentOption[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromHours(1);
            });
            // Search is user-interactive; keep it short so typing-triggered changes don't
            // see stale results for long.
            s.RegisterType<LogItem[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromSeconds(30);
            });
            // Aggregation queries (measure, count, summary-list, single-fingerprint drilldown)
            // — 1 minute is a reasonable fresh-reload cadence that still avoids re-running the
            // same KQL on every page navigation.
            s.RegisterType<MeasureData[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
            s.RegisterType<CountData[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
            s.RegisterType<SummaryData, IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
            s.RegisterType<SummarySubset[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
        });
    }
}