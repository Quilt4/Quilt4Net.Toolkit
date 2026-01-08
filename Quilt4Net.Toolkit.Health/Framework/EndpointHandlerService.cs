using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Features.Health.Dependency;
using Quilt4Net.Toolkit.Features.Health.Live;
using Quilt4Net.Toolkit.Features.Health.Metrics;
using Quilt4Net.Toolkit.Features.Health.Ready;
using Quilt4Net.Toolkit.Features.Health.Version;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Health.Framework;

internal class EndpointHandlerService : IEndpointHandlerService
{
    private readonly ILiveService _liveService;
    private readonly IReadyService _readyService;
    private readonly IHealthService _healthService;
    private readonly IDependencyService _dependencyService;
    private readonly IMetricsService _metricsService;
    private readonly IVersionService _versionService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Quilt4NetHealthApiOptions _apiOptions;

    public EndpointHandlerService(ILiveService liveService, IReadyService readyService, IHealthService healthService, IDependencyService dependencyService, IMetricsService metricsService, IVersionService versionService, IHostEnvironment hostEnvironment, IOptions<Quilt4NetHealthApiOptions> options)
    {
        _liveService = liveService;
        _readyService = readyService;
        _healthService = healthService;
        _dependencyService = dependencyService;
        _metricsService = metricsService;
        _versionService = versionService;
        _hostEnvironment = hostEnvironment;
        _apiOptions = options.Value;
    }

    public async Task<IResult> HandleCall<T>(HealthEndpoint healthEndpoint, HttpContext ctx, T options, CancellationToken cancellationToken) where T : MethodOptions
    {
        switch (healthEndpoint)
        {
            case HealthEndpoint.Live:
                return await LiveAsync(ctx);
            case HealthEndpoint.Ready:
                return await ReadyAsync(ctx, cancellationToken);
            case HealthEndpoint.Health:
                return await HealthAsync(ctx, options as GetMethodOptions, cancellationToken);
            case HealthEndpoint.Dependencies:
                return await DependencyAsync(ctx, options as GetMethodOptions, cancellationToken);
            case HealthEndpoint.Metrics:
                return await MetricsAsync(ctx, options as GetMethodOptions, cancellationToken);
            case HealthEndpoint.Version:
                return await VersionAsync(ctx, options as GetMethodOptions, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(healthEndpoint), $"Unknown {nameof(healthEndpoint)} {healthEndpoint}.");
        }
    }

    private async Task<IResult> LiveAsync(HttpContext ctx)
    {
        var response = await _liveService.GetStatusAsync();
        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");
        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> ReadyAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var responses = await _readyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        var response = responses.ToReadyResponse();
        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        if (response.Status == ReadyStatus.Unready || response.Status == ReadyStatus.Degraded && _apiOptions.FailReadyWhenDegraded)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> HealthAsync(HttpContext ctx, GetMethodOptions options, CancellationToken cancellationToken)
    {
        var responses = await _healthService.GetStatusAsync(null, true, cancellationToken).ToArrayAsync(cancellationToken);

        var noDependencies = ctx.Request.Query.TryGetValue("noDependencies", out var noDependenciesString) && bool.TryParse(noDependenciesString, out var noDependenciesValue) && noDependenciesValue;
        if (!noDependencies)
        {
            var deps = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
            var dependencies = deps.SelectMany(d => d.Value.DependencyComponents.ToDictionary(x => $"{d.Key}.{x.Key}", x => x.Value)).ToArray();
            responses = responses.Concat(dependencies).ToArray();
        }

        var noCertSelfCheck = ctx.Request.Query.TryGetValue("noCertSelfCheck", out var noCertSelfCheckString) && bool.TryParse(noCertSelfCheckString, out var noCertSelfCheckValue) && noCertSelfCheckValue;

        var certificateHealth = noCertSelfCheck ? null : await GetCertificatehealth(ctx);
        if (certificateHealth != null)
        {
            responses = responses.Concat([new KeyValuePair<string, HealthComponent>("CertificateSelf", certificateHealth)]).ToArray();
        }

        var response = responses.ToHealthResponse();

        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
        switch (options.Details ?? (_hostEnvironment.IsProduction() ? DetailsLevel.AuthenticatedOnly : DetailsLevel.Everyone))
        {
            case DetailsLevel.Everyone:
                break;
            case DetailsLevel.AuthenticatedOnly:
                if (!isAuthenticated)
                {
                    response = ClearDetails(response);
                }
                break;
            case DetailsLevel.NoOne:
                response = ClearDetails(response);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.Details), options.Details, null);
        }

        if (response.Status == HealthStatus.Unhealthy)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> DependencyAsync(HttpContext ctx, GetMethodOptions options, CancellationToken cancellationToken)
    {
        var responses = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
        var response = responses.ToDependencyResponse();

        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
        switch (options.Details ?? (_hostEnvironment.IsProduction() ? DetailsLevel.AuthenticatedOnly : DetailsLevel.Everyone))
        {
            case DetailsLevel.Everyone:
                break;
            case DetailsLevel.AuthenticatedOnly:
                if (!isAuthenticated)
                {
                    response = ClearDetails(response);
                }
                break;
            case DetailsLevel.NoOne:
                response = ClearDetails(response);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.Details), options.Details, null);
        }

        if (response.Status == HealthStatus.Unhealthy)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> MetricsAsync(HttpContext ctx, GetMethodOptions options, CancellationToken cancellationToken)
    {
        var response = await _metricsService.GetMetricsAsync(cancellationToken);

        var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
        switch (options.Details ?? (_hostEnvironment.IsProduction() ? DetailsLevel.AuthenticatedOnly : DetailsLevel.Everyone))
        {
            case DetailsLevel.Everyone:
                break;
            case DetailsLevel.AuthenticatedOnly:
                if (!isAuthenticated)
                {
                    response = ClearDetails(response);
                }
                break;
            case DetailsLevel.NoOne:
                response = ClearDetails(response);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.Details), options.Details, null);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> VersionAsync(HttpContext ctx, GetMethodOptions options, CancellationToken cancellationToken)
    {
        var response = await _versionService.GetVersionAsync(cancellationToken);

        var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
        switch (options.Details ?? (_hostEnvironment.IsProduction() ? DetailsLevel.AuthenticatedOnly : DetailsLevel.Everyone))
        {
            case DetailsLevel.Everyone:
                break;
            case DetailsLevel.AuthenticatedOnly:
                if (!isAuthenticated)
                {
                    response = ClearDetails(response);
                }
                break;
            case DetailsLevel.NoOne:
                response = ClearDetails(response);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.Details), options.Details, null);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private static HealthResponse ClearDetails(HealthResponse response)
    {
        return response with
        {
            Components = response.Components.ToDictionary(x => x.Key, x => x.Value with { Details = null })
        };
    }

    private static DependencyResponse ClearDetails(DependencyResponse response)
    {
        return response with
        {
            Components = response.Components.ToDictionary(x => x.Key, x => x.Value with
            {
                DependencyComponents = x.Value.DependencyComponents.ToDictionary(y => y.Key, y => y.Value with { Details = null })
            })
        };
    }

    private static MetricsResponse ClearDetails(MetricsResponse response)
    {
        return response with
        {
            Machine = null,
            Memory = new Memory
            {
                ApplicationMemoryUsageGb = response.Memory.ApplicationMemoryUsageGb
            },
            Processor = new Processor
            {
                CpuTime = response.Processor.CpuTime,
                NumberOfCores = response.Processor.NumberOfCores,
            },
            Storage = null,
            Gpu = null,
        };
    }

    private static VersionResponse ClearDetails(VersionResponse response)
    {
        return response with
        {
            IpAddress = null,
            Machine = null,
            Version = null
        };
    }


    private async Task<HealthComponent> GetCertificatehealth(HttpContext ctx)
    {
        if (!(_apiOptions.Certificate?.SelfCheckEnabled ?? false)) return null;

        var address = _apiOptions.Certificate.SelfCheckUri.NullIfEmpty() ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return new HealthComponent
            {
                Status = HealthStatus.Degraded,
                Details = new Dictionary<string, string>
                {
                    { "message", $"Cannot build absolute uri with '{address}'." }
                }
            };
        }

        var result = await Certificatehelper.GetCertificateHealthAsync(uri, _apiOptions?.Certificate);
        return result;
    }
}