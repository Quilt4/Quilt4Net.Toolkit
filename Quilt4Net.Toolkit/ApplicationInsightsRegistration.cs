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
        services.AddSingleton<IVersionMatrixService, VersionMatrixService>();
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
            // Version matrix scans for the latest version per (app, env). The view is
            // typically refreshed manually; 5 minutes balances freshness with query cost.
            s.RegisterType<VersionMatrixCell[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(5);
            });
            // Host metrics (CPU / memory / disk / network). Operators iterate quickly when
            // investigating, but each refresh runs four KQL queries — 1 minute matches the
            // other aggregation queries above.
            s.RegisterType<MetricSample[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
            // Log-count pivot (service × severity). One KQL query, similar refresh cadence
            // expectations as other aggregations.
            s.RegisterType<LogCountByServiceCell[], IMemory>(x =>
            {
                x.DefaultFreshSpan = TimeSpan.FromMinutes(1);
            });
        });
    }

    /// <summary>
    /// Register the Application Insights client for consumers that pull configurations from
    /// Quilt4Net.Server instead of from their own <c>appsettings.json</c>. The API key is
    /// resolved from <c>Quilt4Net:RemoteConfiguration</c> (or the top-level <c>Quilt4Net:ApiKey</c>
    /// fallback) and must carry the <c>monitor:read</c> scope. Mutually exclusive with
    /// <see cref="AddQuilt4NetApplicationInsightsClient(IHostApplicationBuilder, Action{ApplicationInsightsOptions})"/>:
    /// use one or the other, not both.
    /// </summary>
    public static void AddQuilt4NetApplicationInsightsClientRemote(this IHostApplicationBuilder builder, Action<RemoteConfigurationOptions> options = null)
    {
        builder.Services.AddQuilt4NetApplicationInsightsClientRemote(builder.Configuration, options);
    }

    /// <summary>
    /// Register the Application Insights client for consumers that pull configurations from
    /// Quilt4Net.Server.
    /// </summary>
    public static void AddQuilt4NetApplicationInsightsClientRemote(this IServiceCollection services, IConfiguration configuration, Action<RemoteConfigurationOptions> options = null)
    {
        var apiKey = configuration?.GetSection("Quilt4Net").GetSection("ApiKey").Value;
        var address = configuration?.GetSection("Quilt4Net").GetSection("Quilt4NetAddress").Value;

        var config = configuration?.GetSection("Quilt4Net:RemoteConfiguration").Get<RemoteConfigurationOptions>();
        var o = new RemoteConfigurationOptions
        {
            ApiKey = config?.ApiKey ?? apiKey,
            Quilt4NetAddress = config?.Quilt4NetAddress ?? address ?? "https://quilt4net.com/",
            Ttl = config?.Ttl,
            HttpTimeout = config?.HttpTimeout ?? TimeSpan.FromSeconds(5)
        };

        if (!Uri.TryCreate(o.Quilt4NetAddress, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Configuration {nameof(o.Quilt4NetAddress)} with value '{o.Quilt4NetAddress}' cannot be parsed to an absolute uri.");

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        // Local ApplicationInsightsOptions is empty in remote mode — every call must
        // supply an IApplicationInsightsContext, which the Blazor selector provides
        // from the IApplicationInsightsConfigurationProvider list.
        services.AddSingleton(Options.Create(new ApplicationInsightsOptions()));

        services.AddSingleton<IApplicationInsightsConfigurationProvider, RemoteApplicationInsightsConfigurationProvider>();

        services.AddTransient<IApplicationInsightsService, ApplicationInsightsService>();
        services.AddSingleton<IVersionMatrixService, VersionMatrixService>();
        services.AddTransient<IHealthClient, HealthClient>();

        services.AddCache(s =>
        {
            s.RegisterType<EnvironmentOption[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromHours(1); });
            s.RegisterType<LogItem[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromSeconds(30); });
            s.RegisterType<MeasureData[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
            s.RegisterType<CountData[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
            s.RegisterType<SummaryData, IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
            s.RegisterType<SummarySubset[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
            s.RegisterType<VersionMatrixCell[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(5); });
            s.RegisterType<MetricSample[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
            s.RegisterType<LogCountByServiceCell[], IMemory>(x => { x.DefaultFreshSpan = TimeSpan.FromMinutes(1); });
        });
    }
}