using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Features.Health.Dependency;
using Quilt4Net.Toolkit.Features.Health.Live;
using Quilt4Net.Toolkit.Features.Health.Metrics;
using Quilt4Net.Toolkit.Features.Health.Metrics.Machine;
using Quilt4Net.Toolkit.Features.Health.Ready;
using Quilt4Net.Toolkit.Features.Health.Metrics.Storage;
using Quilt4Net.Toolkit.Features.Health.Version;
using Quilt4Net.Toolkit.Features.Probe;
using Quilt4Net.Toolkit.Health.Framework;
using System.Reflection;
using System.Text;

namespace Quilt4Net.Toolkit.Health;

public static class HealthRegistration
{
    public static IServiceCollection AddQuilt4NetHealthApi(this IHostApplicationBuilder builder, Action<Quilt4NetHealthApiOptions> configure = null)
    {
        return AddQuilt4NetHealthApi(builder.Services, builder.Configuration, builder.Environment, configure);
    }

    /// <summary>
    /// Add API with Health endpoints.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="configure"></param>
    public static IServiceCollection AddQuilt4NetHealthApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, Action<Quilt4NetHealthApiOptions> configure = null)
    {
        var apiOptions = BuildOptions(configuration, environment, configure);

        services.AddSingleton(apiOptions);
        services.AddSingleton(Options.Create(apiOptions));

        services.AddSingleton<IActionDescriptorProvider, CustomRouteDescriptorProvider>();
        services.AddSingleton<IHostedServiceProbeRegistry, HostedServiceProbeRegistry>();

        services.AddTransient<ILiveService, LiveService>();
        services.AddTransient<IReadyService, ReadyService>();
        services.AddTransient<IHealthService, HealthService>();
        services.AddTransient<IDependencyService, DependencyService>();
        services.AddTransient<IVersionService, VersionService>();
        services.AddTransient<IMachineMetricsService, MachineMetricsService>();
        services.AddTransient<IMetricsService, MetricsService>();
        services.AddTransient<IMemoryMetricsService, MemoryMetricsService>();
        services.AddTransient<IStorageMetricsService, StorageMetricsService>();
        services.AddTransient<IProcessorMetricsService, ProcessorMetricsService>();
        services.AddTransient<IGpuMetricsService, GpuMetricsService>();
        services.AddTransient<IHostedServiceProbe, HostedServiceProbe>();
        services.AddTransient<IEndpointHandlerService, EndpointHandlerService>();
        services.AddTransient(typeof(IHostedServiceProbe<>), typeof(HostedServiceProbe<>));

        foreach (var componentServiceType in apiOptions.ComponentServices)
        {
            services.AddTransient(componentServiceType);
        }

        if (apiOptions.ComponentServices.Count() == 1)
        {
            services.AddTransient(s => (IComponentService)s.GetRequiredService(apiOptions.ComponentServices.Single()));
        }

        return services;
    }

    private static Quilt4NetHealthApiOptions BuildOptions(IConfiguration configuration, IHostEnvironment environment, Action<Quilt4NetHealthApiOptions> configure)
    {
        var o = new Quilt4NetHealthApiOptions();

        ApplyEnvironmentDefaults(o, environment);

        configuration.GetSection("Quilt4Net:HealthApi").Bind(o);
        configure?.Invoke(o);

        foreach (HealthEndpoint ep in Enum.GetValues<HealthEndpoint>())
        {
            o.Endpoints.TryAdd(ep, new HealthEndpointOptions());
        }

        EnforceCapabilities(o);

        if (string.IsNullOrEmpty(o.ControllerName))
        {
            o.ControllerName = new Quilt4NetHealthApiOptions().ControllerName;
        }

        if (!string.IsNullOrEmpty(o.Pattern))
        {
            if (!o.Pattern.StartsWith('/')) o.Pattern = $"/{o.Pattern}";
            if (!o.Pattern.EndsWith('/')) o.Pattern = $"{o.Pattern}/";
        }

        return o;
    }

    /// <summary>
    /// Sets up routing to the Quilt4Net health checks.
    /// Must be executed after UseAuthentication and UseAuthorization for authentication to work.
    /// </summary>
    /// <param name="app"></param>
    public static void UseQuilt4NetHealthApi(this WebApplication app)
    {
        var o = app.Services.GetRequiredService<IOptions<Quilt4NetHealthApiOptions>>().Value;
        if (o == null) throw new InvalidOperationException($"Call {nameof(AddQuilt4NetHealthApi)} before {nameof(UseQuilt4NetHealthApi)}.");

        CreaetLogScope(app);
        RegisterEndpoints(o, app, o.DependencyRegistrations.Any());
    }

    private static void CreaetLogScope(WebApplication app)
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        if (assemblyName != null)
        {
            app.Use(async (context, next) =>
            {
                using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                           .CreateLogger("Scope")
                           .BeginScope(new Dictionary<string, object>
                           {
                               ["MachineName"] = Environment.MachineName,
                               ["ApplicationName"] = assemblyName.Name,
                               ["Version"] = assemblyName.Version
                           }))
                {
                    await next(context);
                }
            });
        }
    }

    private static void RegisterEndpoints(Quilt4NetHealthApiOptions o, WebApplication app, bool hasDependencies)
    {
        var basePath = $"{o.Pattern}{o.ControllerName}";

        //NOTE: Map default route
        if (Enum.TryParse<HealthEndpoint>(o.DefaultAction, true, out var defaultKey) && o.Endpoints.TryGetValue(defaultKey, out var defaultItem))
        {
            MapEndpointWithVerb(o, app, defaultKey, basePath, HttpMethods.Get, defaultItem.Get, true);
            MapEndpointWithVerb(o, app, defaultKey, basePath, HttpMethods.Head, defaultItem.Head, true);
        }

        foreach (var item in o.Endpoints)
        {
            var path = $"{basePath}/{item.Key}";

            //If there are no dependencies, do not add that endpoint.
            if (path.EndsWith($"{HealthEndpoint.Dependencies}") && !hasDependencies) continue;

            MapEndpointWithVerb(o, app, item.Key, path, HttpMethods.Get, item.Value.Get);
            MapEndpointWithVerb(o, app, item.Key, path, HttpMethods.Head, item.Value.Head);
        }
    }

    private static void MapEndpointWithVerb<T>(Quilt4NetHealthApiOptions o, WebApplication app, HealthEndpoint healthEndpoint, string path, string verb, T options, bool isDefault = false) where T : MethodOptions
    {
        if ((o.OverrideState ?? options.State) != EndpointState.Disabled)
        {
            var route = app.MapMethods(path, [verb], async (HttpContext ctx, CancellationToken cancellationToken) =>
            {
                if (!(ctx.User.Identity?.IsAuthenticated ?? false))
                {
                    try
                    {
                        var result = await ctx.AuthenticateAsync(o.AuthScheme);
                        if (result.Succeeded && result.Principal != null)
                        {
                            ctx.User = result.Principal;
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Trace.TraceWarning(e.Message);
                    }
                }

                return await HandleCall(healthEndpoint, ctx, options, cancellationToken);
            });

            if ((o.OverrideState ?? options.State) != EndpointState.Visible)
            {
                route.ExcludeFromDescription();
            }
            else
            {
                var documentation = GetDocumentation(o, healthEndpoint, verb);
                if (isDefault)
                {
                    documentation = documentation with { Description = $"This is the *Default* endpoint. It can be configured with mapping to any check. Currently, it uses the **{healthEndpoint}** endpoint.\n\n{documentation.Description}" };
                }

                route.WithSummary(documentation.Summary)
                    .WithDescription(documentation.Description)
                    .WithTags(o.ControllerName);

                foreach (var response in documentation.Responses)
                {
                    if (response.Item2 != null && verb == HttpMethods.Get)
                        route.Produces(response.StatusCode, response.Type, "application/json");
                    else
                        route.Produces(response.StatusCode);
                }
            }
        }

        async Task<IResult> HandleCall<T>(HealthEndpoint endpoint, HttpContext ctx, T opt, CancellationToken cancellationToken) where T : MethodOptions
        {
            var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
            switch (options.Access.Level ?? (app.Environment.IsProduction() ? AccessLevel.AuthenticatedOnly : AccessLevel.Everyone))
            {
                case AccessLevel.Everyone:
                    break;
                case AccessLevel.AuthenticatedOnly:
                    if (!isAuthenticated)
                    {
                        return Results.Unauthorized();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options.Access.Level), options.Access.Level, null);
            }

            var service = app.Services.GetService<IEndpointHandlerService>();
            return await service.HandleCall(endpoint, ctx, opt, cancellationToken);
        }
    }

    private static (string Description, string Summary, (int StatusCode, Type Type)[] Responses) GetDocumentation(Quilt4NetHealthApiOptions o, HealthEndpoint healthEndpoint, string verb)
    {
        switch (healthEndpoint)
        {
            case HealthEndpoint.Live:
                switch (verb)
                {
                    case "GET":
                        return ($"{healthEndpoint} will always return the value *{LiveStatus.Alive}* if it is able to respond.", "Liveness", [(200, typeof(LiveResponse))]);
                    case "HEAD":
                        return ($"{healthEndpoint} will always return the value *{LiveStatus.Alive}* as header value *{nameof(LiveResponse.Status)}* if it is able to respond.", "Liveness", [(200, null)]);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(verb), verb, null);
                }

            case HealthEndpoint.Ready:
                var sb = new StringBuilder();
                if (o.FailReadyWhenDegraded)
                {
                    sb.Append($"{healthEndpoint} checks components that are *{nameof(Component.Essential)}* to figure out if the service is ready or not.\n\nThe response will be ...\n\n- **200** when *{nameof(ReadyStatus.Ready)}*\n\n- **503** when *{nameof(ReadyStatus.Unready)}*\n\n- **503** when {nameof(ReadyStatus.Degraded)}");
                }
                else
                {
                    sb.Append($"{healthEndpoint} checks components that are *{nameof(Component.Essential)}* to figure out if the service is ready or not.\n\nThe response will be ...\n\n- **200** when *{nameof(ReadyStatus.Ready)}*\n\n- **503** when *{nameof(ReadyStatus.Unready)}*\n\n- **200** when {nameof(ReadyStatus.Degraded)}");
                }
                return (sb.ToString(), "Readyness", [(200, typeof(ReadyResponse)), (503, null)]);

            case HealthEndpoint.Health:
                var sbHealth = new StringBuilder();
                sbHealth.Append($"{healthEndpoint} checks the status of the components. By default dependencies are also checked.\n\nDepending on the parameter *{nameof(Component.Essential)}* the response will be...\n\n- **{nameof(HealthStatus.Unhealthy)}** on failure when *{nameof(Component.Essential)}* is true.\n\n- **{nameof(HealthStatus.Degraded)}** on failure when *{nameof(Component.Essential)}* is false\n\n- **{nameof(HealthStatus.Healthy)}** on success");
                switch (verb)
                {
                    case "GET":
                        sbHealth.Append("\n\nThere will also be details for each registered component.");
                        break;
                    case "HEAD":
                        sbHealth.Append($"\n\nThe header value *{nameof(LiveResponse.Status)}* will contain the result.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(verb), verb, null);
                }
                return (sbHealth.ToString(), "Health", [(200, typeof(HealthResponse)), (503, null)]);

            case HealthEndpoint.Dependencies:
                return ($"{healthEndpoint} checks the health dependent components. It does not check the dependencies of the dependent services to protect from circular dependencies.", null, [(200, typeof(DependencyResponse)), (503, null)]);
            case HealthEndpoint.Metrics:
                return ($"{healthEndpoint} returns data about the service.", null, [(200, typeof(MetricsResponse))]);
            case HealthEndpoint.Version:
                return ($"{healthEndpoint} returns metadata about the service like *version*, *environment*, *IpAddress* and more.", null, [(200, typeof(VersionResponse))]);
            default:
                throw new ArgumentOutOfRangeException(nameof(healthEndpoint), healthEndpoint, null);
        }
    }

    private static void ApplyEnvironmentDefaults(Quilt4NetHealthApiOptions o, IHostEnvironment env)
    {
        EnsureAllEndpointsExist(o);

        foreach (var ep in o.Endpoints.Values)
        {
            ep.Head.State = EndpointState.Visible;
            ep.Head.Access.Level = AccessLevel.Everyone;

            ep.Get.State = EndpointState.Visible;
            ep.Get.Access.Level = AccessLevel.Everyone;
            ep.Get.Details = DetailsLevel.AuthenticatedOnly;
        }
        o.Endpoints[HealthEndpoint.Metrics].Get.Access.Level = AccessLevel.AuthenticatedOnly;

        if (env.IsDevelopment())
        {
            foreach (var ep in o.Endpoints.Values)
            {
                ep.Get.Access.Level = AccessLevel.Everyone;
                ep.Get.Details = DetailsLevel.Everyone;
            }
        }
        else if (env.IsProduction())
        {
            foreach (var ep in o.Endpoints.Values)
            {
                ep.Head.State = EndpointState.Hidden;
                ep.Get.State = EndpointState.Hidden;
            }
        }

        // Capabilities
        o.Endpoints[HealthEndpoint.Metrics].Head.State = EndpointState.Disabled;
        o.Endpoints[HealthEndpoint.Version].Head.State = EndpointState.Disabled;
    }

    private static void EnsureAllEndpointsExist(Quilt4NetHealthApiOptions o)
    {
        var keys = Enum.GetValues<HealthEndpoint>();
        foreach (var k in keys)
        {
            o.Endpoints.TryAdd(k, new HealthEndpointOptions());
        }
    }

    private static void EnforceCapabilities(Quilt4NetHealthApiOptions o)
    {
        // Metrics/Version do not support HEAD
        o.Endpoints.TryAdd(HealthEndpoint.Metrics, new HealthEndpointOptions());
        o.Endpoints.TryAdd(HealthEndpoint.Version, new HealthEndpointOptions());

        o.Endpoints[HealthEndpoint.Metrics].Head.State = EndpointState.Disabled;
        o.Endpoints[HealthEndpoint.Version].Head.State = EndpointState.Disabled;
    }

    //private static bool ValidateOptions(Quilt4NetHealthApiOptions o, out string? error)
    //{
    //    error = null;

    //    if (!o.Endpoints.ContainsKey(HealthEndpoint.Live) || !o.Endpoints.ContainsKey(HealthEndpoint.Ready))
    //    {
    //        error = "Endpoints must include at least Live and Ready.";
    //        return false;
    //    }

    //    // Optional: if you want to fail hard if someone tries to enable HEAD:
    //    if (o.Endpoints.TryGetValue(HealthEndpoint.Metrics, out var metrics) &&
    //        metrics.Head.State != EndpointState.Disabled)
    //    {
    //        error = "Metrics endpoint does not support HEAD. Set Metrics:Head:State to Disabled.";
    //        return false;
    //    }

    //    return true;
    //}
}