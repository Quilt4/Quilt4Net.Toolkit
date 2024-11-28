using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Version;
using Microsoft.AspNetCore.Http;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Health Controller
/// </summary>
public class HealthController : ControllerBase
{
    private readonly ILiveService _liveService;
    private readonly IReadyService _readyService;
    private readonly IHealthService _healthService;
    private readonly IVersionService _versionService;
    private readonly IMetricsService _metricsService;
    private readonly Quilt4NetApiOptions _options;

    private HttpContext _httpContext;

    /// <summary>
    /// Health Controller constructor.
    /// </summary>
    /// <param name="liveService"></param>
    /// <param name="readyService"></param>
    /// <param name="healthService"></param>
    /// <param name="versionService"></param>
    /// <param name="metricsService"></param>
    /// <param name="options"></param>
    public HealthController(ILiveService liveService, IReadyService readyService, IHealthService healthService, IVersionService versionService, IMetricsService metricsService, Quilt4NetApiOptions options)
    {
        _liveService = liveService;
        _readyService = readyService;
        _healthService = healthService;
        _versionService = versionService;
        _metricsService = metricsService;
        _options = options;
    }

    internal new HttpContext HttpContext
    {
        get => _httpContext ?? base.HttpContext;
        set => _httpContext = value;
    }

    /// <summary>
    /// Purpose: Checks if the application is running (basic process check). It should return 200 OK if the service is up, regardless of its ability to handle requests.
    /// Use Case: Typically used by Kubernetes liveness probes to restart the container if it becomes unresponsive.
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> Live()
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
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var responses = await _readyService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        var response = responses.ToReadyResponse();
        //var response = new ReadyResponse
        //{
        //    Status = ReadyStatus.Unready,
        //    Components = responses.ToDictionary(x => x.Key, x => x.Value)
        //};

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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var responses = await _healthService.GetStatusAsync(cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);

        var response = responses.ToHealthResponse();

        HttpContext.Response.Headers.TryAdd(nameof(response.Status), $"{response.Status}");

        if (response.Status == HealthStatus.Unhealthy)
        {
            return HttpContext.Request.Method == HttpMethods.Head ? StatusCode(503) : StatusCode(503, response);
        }

        return HttpContext.Request.Method == HttpMethods.Head ? Ok() : Ok(response);
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
    /// Purpose: Provides metrics for monitoring and observability systems like Prometheus. The format is often specific to the monitoring tool (e.g., Prometheus metrics format).
    /// Use Case: Used to track detailed performance metrics, such as request rates, error rates, CPU/memory usage, and more.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    public async Task<IActionResult> Version(CancellationToken cancellationToken)
    {
        var result = await _versionService.GetVersionAsync(cancellationToken);
        return Ok(result);
    }

    ///// <summary>
    ///// Purpose: Lists all critical dependencies and their statuses.
    ///// Use Case: Provides a higher-level overview of the service’s environment and dependency health.
    ///// </summary>
    ///// <returns></returns>
    //public IActionResult Dependencies()
    //{
    //    /*
    //    {
    //      "dependencies": {
    //        "database": { "status": "healthy" },
    //        "messageQueue": { "status": "degraded" },
    //        "authService": { "status": "healthy" }
    //      }
    //    }
    //     */

    //    throw new NotImplementedException();
    //}
}