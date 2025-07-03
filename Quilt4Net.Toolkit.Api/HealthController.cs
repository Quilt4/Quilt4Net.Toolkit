using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Api.Features.Dependency;
using Quilt4Net.Toolkit.Features.Health;
using Microsoft.AspNetCore.Connections.Features;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Health Controller
/// </summary>
[ApiController]
[Route("Api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILiveService _liveService;
    private readonly IReadyService _readyService;
    private readonly IHealthService _healthService;
    private readonly IVersionService _versionService;
    private readonly IMetricsService _metricsService;
    private readonly IDependencyService _dependencyService;
    private readonly Quilt4NetApiOptions _options;

    private HttpContext _httpContext;

    /// <summary>
    /// Health Controller constructor.
    /// </summary>
    /// <param name="hostEnvironment"></param>
    /// <param name="liveService"></param>
    /// <param name="readyService"></param>
    /// <param name="healthService"></param>
    /// <param name="versionService"></param>
    /// <param name="metricsService"></param>
    /// <param name="dependencyService"></param>
    /// <param name="options"></param>
    public HealthController(IHostEnvironment hostEnvironment, ILiveService liveService, IReadyService readyService, IHealthService healthService, IVersionService versionService, IMetricsService metricsService, IDependencyService dependencyService, Quilt4NetApiOptions options)
    {
        _hostEnvironment = hostEnvironment;
        _liveService = liveService;
        _readyService = readyService;
        _healthService = healthService;
        _versionService = versionService;
        _metricsService = metricsService;
        _dependencyService = dependencyService;
        _options = options;
    }

    internal new HttpContext HttpContext
    {
        get => _httpContext ?? base.HttpContext;
        set => _httpContext = value;
    }

    [HttpGet]
    [HttpHead]
    public async Task<IActionResult> Default(CancellationToken cancellationToken)
    {
        switch (_options.DefaultAction.ToLower())
        {
            case "live":
                return await Live(cancellationToken);
            case "ready":
                return await Ready(cancellationToken);
            case "health":
                return await Health(false, cancellationToken);
            case "dependencies":
                return await Dependencies(cancellationToken);
            case "metrics":
                return await Metrics(cancellationToken);
            case "version":
                return await Version(cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.DefaultAction), $"Unknown configuration {nameof(_options.DefaultAction)} {_options.DefaultAction}.");
        }
    }

    /// <summary>
    /// Purpose: Checks if the application is running (basic process check). It should return 200 OK if the service is up, regardless of its ability to handle requests.
    /// Use Case: Typically used by Kubernetes liveness probes to restart the container if it becomes unresponsive.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [HttpHead]
    [Route("Live")]
    public async Task<IActionResult> Live(CancellationToken cancellationToken)
    {
        var response = await _liveService.GetStatusAsync();
        HttpContext.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        return HttpContext.Request.Method == HttpMethods.Head ? Ok() : Ok(response);
    }

    /// <summary>
    /// Purpose: Indicates whether the application is ready to handle traffic, including checks for essential dependencies (e.g., database, cache, APIs).
    /// Use Case: Used by Kubernetes readiness probes to determine if the service should be added to the load balancer.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [HttpHead]
    [Route("Ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var responses = await _readyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        var response = responses.ToReadyResponse();

        HttpContext.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        if (response.Status == ReadyStatus.Unready || (response.Status == ReadyStatus.Degraded && _options.FailReadyWhenDegraded))
        {
            return HttpContext.Request.Method == HttpMethods.Head ? StatusCode(503) : StatusCode(503, response);
        }

        return HttpContext.Request.Method == HttpMethods.Head ? Ok() : Ok(response);
    }

    /// <summary>
    /// Purpose: Provides detailed information about the health of the service and its dependencies. This can include overall status and specific details about databases, queues, external APIs, etc.
    /// Use Case: Primarily used for monitoring systems like Prometheus, Grafana, or custom dashboards to track application health.
    /// </summary>
    /// <param name="noDependencies"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [HttpHead]
    [Route("Health")]
    public async Task<IActionResult> Health([FromQuery] bool noDependencies, CancellationToken cancellationToken)
    {
        var responses = await _healthService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);

        if (!noDependencies)
        {
            var deps = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
            var dependencies = deps.SelectMany(d => d.Value.DependencyComponents.ToDictionary(x => $"{d.Key}.{x.Key}", x => x.Value)).ToArray();
            responses = responses.Concat(dependencies).ToArray();
        }

        var certificateHealth = await GetCertificatehealth();
        if (certificateHealth != null)
        {
            responses = responses.Concat([new KeyValuePair<string, HealthComponent>("CertificateSelf", certificateHealth)]).ToArray();
        }

        var response = responses.ToHealthResponse();

        HttpContext.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        var isAuthenticated = HttpContext.User.Identity?.Name != null;
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
            return HttpContext.Request.Method == HttpMethods.Head ? StatusCode(503) : StatusCode(503, response);
        }

        return HttpContext.Request.Method == HttpMethods.Head ? Ok() : Ok(response);
    }

    private async Task<HealthComponent> GetCertificatehealth()
    {
        if (!(_options.Certificate?.SelfCheckEnabled ?? false)) return null;

        //var tlsFeature = HttpContext.Features.Get<ITlsHandshakeFeature>();
        //if (tlsFeature != null)
        //{
        //    var protocol = tlsFeature.Protocol; // Tls12, Tls13, etc.
        //}

        //var connection = HttpContext.Connection;
        //var localAddress = connection.LocalIpAddress?.ToString();
        //var localPort = connection.LocalPort;
        var scheme = HttpContext.Request.Scheme; // "https" or "http"
        if (!Uri.TryCreate($"{scheme}://{HttpContext.Request.Host}", UriKind.Absolute, out var uri)) return null;

        var result = await Certificatehelper.GetCertificateHealthAsync(uri, _options?.Certificate);
        return result;
    }

    private static HealthResponse ClearDetails(HealthResponse response)
    {
        return response with
        {
            Components = response.Components.ToDictionary(x => x.Key, x => x.Value with { Details = default })
        };
    }

    ///// <summary>
    ///// Purpose: Indicates whether the application has completed its startup routine and is ready to perform other checks. This is especially useful for containerized or microservice-based applications.
    ///// Use Case: Prevents health checks or traffic redirection until the application is fully initialized.
    ///// </summary>
    ///// <returns></returns>
    //public IActionResult Startup()
    //{
    //    return Ok(new { status = "started" });
    //}

    /// <summary>
    /// Purpose: Lists all critical dependencies and their statuses.
    /// Use Case: Provides a higher-level overview of the service’s environment and dependency health.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [HttpHead]
    [Route("Dependencies")]
    public async Task<IActionResult> Dependencies(CancellationToken cancellationToken)
    {
        var responses = await _dependencyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken);
        var response = responses.ToDependencyResponse();

        HttpContext.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        if (response.Status == HealthStatus.Unhealthy)
        {
            return HttpContext.Request.Method == HttpMethods.Head ? StatusCode(503) : StatusCode(503, response);
        }

        return HttpContext.Request.Method == HttpMethods.Head ? Ok() : Ok(response);
    }

    /// <summary>
    /// Purpose: Provides metrics for monitoring and observability systems like Prometheus. The format is often specific to the monitoring tool (e.g., Prometheus metrics format).
    /// Use Case: Used to track detailed performance metrics, such as request rates, error rates, CPU/memory usage, and more.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("Metrics")]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        var metrics = await _metricsService.GetMetricsAsync(cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Purpose: Provides the version of the application or service. This can be helpful for debugging or ensuring proper deployments.
    /// Use Case: Enables operators to quickly verify the deployed version.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Version")]
    public async Task<IActionResult> Version(CancellationToken cancellationToken)
    {
        var result = await _versionService.GetVersionAsync(cancellationToken);
        return Ok(result);
    }
}