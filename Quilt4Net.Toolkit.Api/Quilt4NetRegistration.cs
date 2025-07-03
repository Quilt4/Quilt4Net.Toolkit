using Microsoft.AspNetCore.Mvc.Abstractions;
using Quilt4Net.Toolkit.Api.Features.Dependency;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Probe;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Api.Framework;
using System.Reflection;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Quilt4Net service registration.
/// </summary>
public static class Quilt4NetRegistration
{
    private static Quilt4NetApiOptions _options;

    /// <summary>
    /// Register using WebApplicationBuilder.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApi(this WebApplicationBuilder builder, Action<Quilt4NetApiOptions> options = default)
    {
        AddQuilt4NetApi(builder.Services, builder.Configuration, options);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApi(this IServiceCollection services, Action<Quilt4NetApiOptions> options = default)
    {
        AddQuilt4NetApi(services, default, options);
    }

    /// <summary>
    /// Register using IServiceCollection and optional IConfiguration.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApi(this IServiceCollection services, IConfiguration configuration, Action<Quilt4NetApiOptions> options = default)
    {
        _options = BuildOptions(configuration, options);
        services.AddSingleton(_ => _options);

        services.AddControllers(o => { o.Conventions.Add(new CustomRouteConvention(_options)); });

        services.AddSingleton<IActionDescriptorProvider, CustomRouteDescriptorProvider>();
        services.AddSingleton<IHostedServiceProbeRegistry, HostedServiceProbeRegistry>();

        services.AddTransient<ILiveService, LiveService>();
        services.AddTransient<IReadyService, ReadyService>();
        services.AddTransient<IHealthService, HealthService>();
        services.AddTransient<IDependencyService, DependencyService>();
        services.AddTransient<IVersionService, VersionService>();
        services.AddTransient<IMetricsService, MetricsService>();
        services.AddTransient<IMemoryMetricsService, MemoryMetricsService>();
        services.AddTransient<IProcessorMetricsService, ProcessorMetricsService>();
        services.AddTransient<IHostedServiceProbe, HostedServiceProbe>();
        services.AddTransient(typeof(IHostedServiceProbe<>), typeof(HostedServiceProbe<>));

        foreach (var componentServices in _options.ComponentServices)
        {
            services.AddTransient(componentServices);
        }

        if (_options.ComponentServices.Count() == 1)
        {
            services.AddTransient(s => (IComponentService)s.GetService(_options.ComponentServices.Single()));
        }
    }

    private static Quilt4NetApiOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetApiOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net:Api").Get<Quilt4NetApiOptions>() ?? new Quilt4NetApiOptions();
        options?.Invoke(o);

        //NOTE: the pattern needs to start and end with '/'.
        if (!o.Pattern.EndsWith('/')) o.Pattern = $"{o.Pattern}/";
        if (!o.Pattern.StartsWith('/')) o.Pattern = $"/{o.Pattern}";

        //NOTE: Empty controller name is not allowed, automatically revert to default.
        if (string.IsNullOrEmpty(o.ControllerName)) o.ControllerName = new Quilt4NetApiOptions().ControllerName;

        return o;
    }

    /// <summary>
    /// Sets up routing to the Quilt4Net health checks.
    /// </summary>
    /// <param name="app"></param>
    public static void UseQuilt4NetApi(this WebApplication app)
    {
        if (_options == null) throw new InvalidOperationException($"Call {nameof(AddQuilt4NetApi)} before {nameof(UseQuilt4NetApi)}.");

        if (_options.Logging?.UseCorrelationId ?? false)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        _options.ShowInOpenApi ??= !app.Services.GetService<IHostEnvironment>().IsProduction();

        if ((_options.Logging?.LogHttpRequest ?? HttpRequestLogMode.None) > HttpRequestLogMode.None)
        {
            app.UseWhen(
                //context => context.Request.Path.StartsWithSegments("/Api"),
                _ => true,
                branch =>
                {
                    branch.UseMiddleware<RequestResponseLoggingMiddleware>();
                }
            );
        }

        var asm = Assembly.GetEntryAssembly();
        var nm = asm?.GetName();
        if (nm != null)
        {
            app.Use(async (context, next) =>
            {
                using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                           .CreateLogger("Scope")
                           .BeginScope(new Dictionary<string, object>
                           {
                               ["ApplicationName"] = nm.Name,
                               ["Version"] = nm.Version
                           }))
                {
                    await next(context);
                }
            });
        }
    }
}