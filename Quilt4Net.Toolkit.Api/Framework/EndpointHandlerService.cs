using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Features.Dependency;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Api.Framework.Endpoints;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Framework;

internal class EndpointHandlerService : IEndpointHandlerService
{
    private readonly ILiveService _liveService;
    private readonly IReadyService _readyService;
    private readonly IHealthService _healthService;
    private readonly IDependencyService _dependencyService;
    private readonly IMetricsService _metricsService;
    private readonly IVersionService _versionService;
    private readonly IHostEnvironment _hostEnvironment;
    private static Quilt4NetApiOptions _options;

    public EndpointHandlerService(ILiveService liveService, IReadyService readyService, IHealthService healthService, IDependencyService dependencyService, IMetricsService metricsService, IVersionService versionService, IHostEnvironment hostEnvironment, IOptions<Quilt4NetApiOptions> options)
    {
        _liveService = liveService;
        _readyService = readyService;
        _healthService = healthService;
        _dependencyService = dependencyService;
        _metricsService = metricsService;
        _versionService = versionService;
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public async Task<IResult> HandleCall(string path, string basePath, HttpContext ctx, CancellationToken cancellationToken)
    {
        var action = path.Replace(basePath, string.Empty).TrimStart('/');
        if (action == "") action = _options.DefaultAction;
        if (!Enum.TryParse<HealthEndpoint>(action, true, out var healthEndpoint)) throw new InvalidOperationException($"Cannot parse {action} to {nameof(HealthEndpoint)}.");

        switch (healthEndpoint)
        {
            case HealthEndpoint.Default:
                throw new NotSupportedException($"This {nameof(healthEndpoint)} should already have been replaced with the actual {nameof(HealthEndpoint.Default)}.");
            case HealthEndpoint.Live:
                return await LiveAsync(ctx);
            case HealthEndpoint.Ready:
                return await ReadyAsync(ctx, cancellationToken);
            case HealthEndpoint.Health:
                return await HealthAsync(ctx, cancellationToken);
            case HealthEndpoint.Dependencies:
                return await DependencyAsync(ctx, cancellationToken);
            case HealthEndpoint.Metrics:
                return await MetricsAsync(ctx, cancellationToken);
            case HealthEndpoint.Version:
                return await VersionAsync(ctx, cancellationToken);
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

        if (response.Status == ReadyStatus.Unready || response.Status == ReadyStatus.Degraded && _options.FailReadyWhenDegraded)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> HealthAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var responses = await _healthService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);

        var noDependencies = ctx.Request.Query.TryGetValue("noDependencies", out var value) && bool.TryParse(value, out var parsed) && parsed;
        if (!noDependencies)
        {
            var deps = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
            var dependencies = deps.SelectMany(d => d.Value.DependencyComponents.ToDictionary(x => $"{d.Key}.{x.Key}", x => x.Value)).ToArray();
            responses = responses.Concat(dependencies).ToArray();
        }

        var certificateHealth = await GetCertificatehealth(ctx);
        if (certificateHealth != null)
        {
            responses = responses.Concat([new KeyValuePair<string, HealthComponent>("CertificateSelf", certificateHealth)]).ToArray();
        }

        var response = responses.ToHealthResponse();

        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        var isAuthenticated = ctx.User.Identity?.Name != null;
        switch (_options.AuthDetail ?? (_hostEnvironment.IsProduction() ? AuthDetailLevel.AuthenticatedOnly : AuthDetailLevel.EveryOne))
        {
            case AuthDetailLevel.EveryOne:
                break;
            case AuthDetailLevel.AuthenticatedOnly:
                if (!isAuthenticated)
                {
                    response = ClearDetails(response);
                }
                break;
            case AuthDetailLevel.NoOne:
                response = ClearDetails(response);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.AuthDetail), _options.AuthDetail, null);
        }

        if (response.Status == HealthStatus.Unhealthy)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> DependencyAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var responses = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
        var response = responses.ToDependencyResponse();

        ctx.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        if (response.Status == HealthStatus.Unhealthy)
        {
            return ctx.Request.Method == HttpMethods.Head ? Results.StatusCode(503) : Results.Json(response, statusCode: 503);
        }

        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> MetricsAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var response = await _metricsService.GetMetricsAsync(cancellationToken);
        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private async Task<IResult> VersionAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var response = await _versionService.GetVersionAsync(cancellationToken);
        return ctx.Request.Method == HttpMethods.Head ? Results.Ok() : Results.Ok(response);
    }

    private static HealthResponse ClearDetails(HealthResponse response)
    {
        return response with
        {
            Components = response.Components.ToDictionary(x => x.Key, x => x.Value with { Details = default })
        };
    }

    private static async Task<HealthComponent> GetCertificatehealth(HttpContext ctx)
    {
        if (!(_options.Certificate?.SelfCheckEnabled ?? false)) return null;

        var scheme = ctx.Request.Scheme;
        if (!Uri.TryCreate($"{scheme}://{ctx.Request.Host}", UriKind.Absolute, out var uri)) return null;

        var result = await Certificatehelper.GetCertificateHealthAsync(uri, _options?.Certificate);
        return result;
    }
}