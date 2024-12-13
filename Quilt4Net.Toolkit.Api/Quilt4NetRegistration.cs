using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Api.Features.Dependency;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Probe;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Api.Framework;

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

        if (_options.ShowInSwagger)
        {
            services.AddSwaggerGen(c => { c.DocumentFilter<Quilt4NetControllerFilter>(); });
        }

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
        if (_options.UseCorrelationId)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        if (_options.LogHttpRequest > 0)
        {
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }

        app.UseEndpoints(endpoints =>
        {
            var methods = typeof(HealthController).GetMethods()
                .Where(m => m.DeclaringType == typeof(HealthController) && !m.IsSpecialName);

            foreach (var method in methods)
            {
                var routeName = method.Name.ToLower();
                endpoints.MapControllerRoute(
                    name: $"Quilt4Net{routeName}Route",
                    pattern: $"{_options.Pattern}{_options.ControllerName}/{routeName}",
                    defaults: new { controller = _options.ControllerName, action = method.Name }
                );

                //NOTE: Also add the default endpoint
                if (method.Name.Equals(_options.DefaultAction, StringComparison.InvariantCultureIgnoreCase))
                {
                    endpoints.MapControllerRoute(
                        name: $"Quilt4Net{routeName}Route_default",
                        pattern: $"{_options.Pattern}{_options.ControllerName}",
                        defaults: new { controller = _options.ControllerName, action = method.Name }
                    );
                }
            }
        });
    }
}