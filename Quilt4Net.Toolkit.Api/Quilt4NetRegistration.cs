using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Features.Dependency;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Probe;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Api.Framework;
using Quilt4Net.Toolkit.Api.Framework.Endpoints;
using Quilt4Net.Toolkit.Features.Health;
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
    public static void AddQuilt4NetApi(this WebApplicationBuilder builder, Action<Quilt4NetApiOptions> options = null)
    {
        AddQuilt4NetApi(builder.Services, builder.Configuration, options);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApi(this IServiceCollection services, Action<Quilt4NetApiOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
        AddQuilt4NetApi(services, configuration, options);
    }

    /// <summary>
    /// Register using IServiceCollection and optional IConfiguration.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApi(this IServiceCollection services, IConfiguration configuration, Action<Quilt4NetApiOptions> options = null)
    {
        _options = BuildOptions(configuration, options);
        services.AddSingleton(_ => _options);
        services.AddSingleton(Options.Create(_options));
        services.AddSingleton<Quilt4NetServerOptions>(_ => _options);
        services.AddSingleton<IOptions<Quilt4NetServerOptions>>(Options.Create(_options));

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
        services.AddTransient<IEndpointHandlerService, EndpointHandlerService>();
        services.AddTransient(typeof(IHostedServiceProbe<>), typeof(HostedServiceProbe<>));
        services.AddSingleton(_ => new CompiledLoggingOptions(_options));

        services.AddRemoteConfiguration(s => s.GetService<IHostEnvironment>().EnvironmentName);
        services.AddContent(s => s.GetService<IHostEnvironment>().EnvironmentName);

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

        var oRoot = configuration?.GetSection("Quilt4Net").Get<Quilt4NetServerOptions>();
        o.ApiKey ??= oRoot?.ApiKey;
        o.Address ??= oRoot?.Address;
        o.Ttl ??= oRoot?.Ttl;

        //NOTE: Empty controller name is not allowed, automatically revert to default.
        if (string.IsNullOrEmpty(o.ControllerName)) o.ControllerName = new Quilt4NetApiOptions().ControllerName;

        options?.Invoke(o);

        //NOTE: the pattern needs to start and end with '/'.
        if (!o.Pattern.EndsWith('/')) o.Pattern = $"{o.Pattern}/";
        if (!o.Pattern.StartsWith('/')) o.Pattern = $"/{o.Pattern}";

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

        RegisterLoggingMiddleware(app);
        CreaetLogScope(app);
        RegisterEndpoints(app, _options.Dependencies.Any());
    }

    private static void RegisterLoggingMiddleware(WebApplication app)
    {
        if ((_options.Logging?.LogHttpRequest ?? HttpRequestLogMode.None) > HttpRequestLogMode.None)
        {
            app.UseWhen(
                _ => true,
                branch =>
                {
                    branch.UseMiddleware<RequestResponseLoggingMiddleware>();
                }
            );
        }
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
                               ["ApplicationName"] = assemblyName.Name,
                               ["Version"] = assemblyName.Version
                           }))
                {
                    await next(context);
                }
            });
        }
    }

    private static void RegisterEndpoints(WebApplication app, bool hasDependencies)
    {
        //app.MapGet("/openapi.json", async (IApiDescriptionGroupCollectionProvider provider, OpenApiDocumentGenerator generator) =>
        //{
        //    var apiDescriptions = provider.ApiDescriptionGroups;
        //    var document = generator.CreateDocument(apiDescriptions);
        //    return Results.Json(document);
        //});

        var basePath = $"{_options.Pattern}{_options.ControllerName}";
        var accessMap = AccessHelper.Decode(_options.Endpoints ?? "");
        foreach (var (endpoint, flags) in accessMap)
        {
            var path = endpoint == HealthEndpoint.Default
                ? basePath
                : $"{basePath}/{endpoint}";

            if (!flags.Get && !flags.Head) continue;
            if (path.EndsWith("Dependencies") && !hasDependencies) continue; //If there are no dependencies, do not add that endpoint.

            var actionEndpoint = path.Replace(basePath, string.Empty).TrimStart('/');
            var action = actionEndpoint == "" ? _options.DefaultAction : actionEndpoint;
            if (!Enum.TryParse<HealthEndpoint>(action, true, out var healthEndpoint)) throw new InvalidOperationException($"Cannot parse {action} to {nameof(HealthEndpoint)}.");

            var documentation = GetDocumentation(healthEndpoint);
            if (actionEndpoint == "")
            {
                documentation = documentation with { Description = $"This is the *{HealthEndpoint.Default}* endpoint. It can be configured with mapping to any check. Currently it uses the **{healthEndpoint}** endpoint.\n\n{documentation.Description}" };
            }

            var httpMethods = GetVerbs(flags);
            RouteHandlerBuilder route;
            if (healthEndpoint == HealthEndpoint.Health)
            {
                route = app.MapMethods(path, httpMethods, async (HttpContext ctx, CancellationToken cancellationToken, bool noDependencies = false, bool noCertSelfCheck = false) => await HandleCall(healthEndpoint, ctx, cancellationToken));
            }
            else
            {
                route = app.MapMethods(path, httpMethods, async (HttpContext ctx, CancellationToken cancellationToken) => await HandleCall(healthEndpoint, ctx, cancellationToken));
            }

            route.WithSummary(documentation.Summary)
                .WithDescription(documentation.Description)
                .WithTags(_options.ControllerName);

            foreach (var response in documentation.Responses)
            {
                //TODO: Do not return examples until enums are serialized correctly.
                //if (response.Item2 != null)
                //    route.Produces(response.StatusCode, response.Type, "application/json");
                //else
                    route.Produces(response.StatusCode);
            }

            if (!flags.Visible) route.ExcludeFromDescription();
        }

        async Task<IResult> HandleCall(HealthEndpoint path, HttpContext ctx, CancellationToken cancellationToken)
        {
            var service = app.Services.GetService<IEndpointHandlerService>();
            return await service.HandleCall(path, ctx, cancellationToken);
        }
    }

    private static (string Description, string Summary, (int StatusCode,Type Type)[] Responses) GetDocumentation(HealthEndpoint healthEndpoint)
    {
        switch (healthEndpoint)
        {
            case HealthEndpoint.Default:
                throw new NotSupportedException($"This {nameof(healthEndpoint)} should already have been replaced with the actual {nameof(HealthEndpoint.Default)}.");
            case HealthEndpoint.Live:
                return ($"{healthEndpoint} will always return the value *{LiveStatus.Alive}* if it is able to respond.", "Liveness", [(200, typeof(LiveResponse))]);
            case HealthEndpoint.Ready:
                //if (response.Status == ReadyStatus.Unready || response.Status == ReadyStatus.Degraded && _options.FailReadyWhenDegraded)
                //return ($"{healthEndpoint} checks components that are *{nameof(Component.Essential)}* (or *{nameof(Component.NeededToBeReady)}*) to figure out if the service is ready or not.\n\nThe response will be ...\n\n- **200** when *{nameof(ReadyStatus.Ready)}*\n\n- **503** when *{nameof(ReadyStatus.Unready)}*\n\n- **200** when {nameof(ReadyStatus.Degraded)} and option *{nameof(Quilt4NetApiOptions.FailReadyWhenDegraded)}* is false (Default)\n\n- **503** when {nameof(ReadyStatus.Degraded)} and option *{nameof(Quilt4NetApiOptions.FailReadyWhenDegraded)}* is true", "Readyness", [(200, typeof(ReadyResponse)), (503, null)]);
                return ($"{healthEndpoint} checks components that are *{nameof(Component.Essential)}* to figure out if the service is ready or not.\n\nThe response will be ...\n\n- **200** when *{nameof(ReadyStatus.Ready)}*\n\n- **503** when *{nameof(ReadyStatus.Unready)}*\n\n- **200** when {nameof(ReadyStatus.Degraded)} and option *{nameof(Quilt4NetApiOptions.FailReadyWhenDegraded)}* is false (Default)\n\n- **503** when {nameof(ReadyStatus.Degraded)} and option *{nameof(Quilt4NetApiOptions.FailReadyWhenDegraded)}* is true", "Readyness", [(200, typeof(ReadyResponse)), (503, null)]);
            case HealthEndpoint.Health:
                //TODO: needs to add parameters so that the query-parameter noDependencies and noCertSelfCheck will also be documented.
                return ($"{healthEndpoint} checks the status of the components. By default dependencies are also checked.\n\nDepending on the parameter *{nameof(Component.Essential)}* the response will be...\n\n- **{nameof(HealthStatus.Unhealthy)}** on failure when *{nameof(Component.Essential)}* is true.\n\n- **{nameof(HealthStatus.Degraded)}** on failure when *{nameof(Component.Essential)}* is false\n\n- **{nameof(HealthStatus.Healthy)}** on success", "Health", [(200, typeof(ReadyResponse)), (503, null)]);
            case HealthEndpoint.Dependencies:
                return ($"{healthEndpoint} checks the health dependent components. It does not check the dependencies of the dependent services to protect from circular dependencies.", null, [(200, typeof(ReadyResponse)), (503, null)]);
            case HealthEndpoint.Metrics:
                return ($"{healthEndpoint} returns data about the service.", null, [(200, typeof(ReadyResponse))]);
            case HealthEndpoint.Version:
                return ($"{healthEndpoint} returns metadata about the service like *version*, *environment*, *IpAddress* and more.", null, [(200, typeof(ReadyResponse))]);
            default:
                throw new ArgumentOutOfRangeException(nameof(healthEndpoint), healthEndpoint, null);
        }
    }

    private static IEnumerable<string> GetVerbs(AccessFlags flags)
    {
        if (flags.Get) yield return "GET";
        if (flags.Head) yield return "HEAD";
    }
}

//public class OpenApiDocumentGenerator
//{
//    public OpenApiDocument CreateDocument(ApiDescriptionGroupCollection apiGroups)
//    {
//        var doc = new OpenApiDocument
//        {
//            Info = new OpenApiInfo
//            {
//                Title = "Your API",
//                Version = "v1"
//            },
//            Paths = new OpenApiPaths()
//        };

//        foreach (var group in apiGroups.Items)
//        {
//            foreach (var api in group.Items)
//            {
//                var path = "/" + api.RelativePath.TrimEnd('/');
//                if (!doc.Paths.ContainsKey(path))
//                {
//                    doc.Paths[path] = new OpenApiPathItem();
//                }

//                doc.Paths[path].Operations[OperationType.Get] = new OpenApiOperation
//                {
//                    Summary = api.ActionDescriptor.DisplayName,
//                    Responses = new OpenApiResponses
//                    {
//                        ["200"] = new OpenApiResponse { Description = "Success" }
//                    }
//                };
//            }
//        }

//        return doc;
//    }
//}