using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Ready;

namespace Quilt4Net.Toolkit.Api;

public class HealthController : ControllerBase
{
    private readonly ILiveService _liveService;
    private readonly IReadyService _readyService;
    private readonly IHealthService _healthService;

    public HealthController(ILiveService liveService, IReadyService readyService, IHealthService healthService)
    {
        _liveService = liveService;
        _readyService = readyService;
        _healthService = healthService;
    }

    /// <summary>
    /// Purpose: Checks if the application is running (basic process check). It should return 200 OK if the service is up, regardless of its ability to handle requests.
    /// Use Case: Typically used by Kubernetes liveness probes to restart the container if it becomes unresponsive.
    /// </summary>
    /// <returns>alive</returns>
    public async Task<IActionResult> Live()
    {
        return Ok(await _liveService.GetStatusAsync());
    }

    /// <summary>
    /// Purpose: Indicates whether the application is ready to handle traffic, including checks for essential dependencies (e.g., database, cache, APIs).
    /// Use Case: Used by Kubernetes readiness probes to determine if the service should be added to the load balancer.
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var result = await _readyService.GetStatusAsync(cancellationToken);

        //TODO: Configure if traffic is accepted on Degraded
        if (result.Status == ReadyStatusResult.Unready)
        {
            return StatusCode(503, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Purpose: Provides detailed information about the health of the service and its dependencies. This can include overall status and specific details about databases, queues, external APIs, etc.
    /// Use Case: Primarily used for monitoring systems like Prometheus, Grafana, or custom dashboards to track application health.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await _healthService.GetStatusAsync(cancellationToken);

        if (result.Status == HealthStatusResult.Unhealthy)
        {
            return StatusCode(503, result);
        }

        return Ok(result);
    }

    ///// <summary>
    ///// Purpose: Indicates whether the application has completed its startup routine and is ready to perform other checks. This is especially useful for containerized or microservice-based applications.
    ///// Use Case: Prevents health checks or traffic redirection until the application is fully initialized.
    ///// </summary>
    ///// <returns></returns>
    //public IActionResult Startup()
    //{
    //    //TODO: Implement
    //    return Ok(new { status = "started" });
    //}

    ///// <summary>
    ///// Purpose: Provides metrics for monitoring and observability systems like Prometheus. The format is often specific to the monitoring tool (e.g., Prometheus metrics format).
    ///// Use Case: Used to track detailed performance metrics, such as request rates, error rates, CPU/memory usage, and more.
    ///// </summary>
    ///// <returns></returns>
    ///// <exception cref="NotImplementedException"></exception>
    //public IActionResult Metrics()
    //{
    //    //TODO: Implement
    //    /*
    //        http_requests_total{method="GET",endpoint="/api/values"} 1027
    //        memory_usage_bytes 52428800
    //    */
    //    throw new NotImplementedException();
    //}

    ///// <summary>
    ///// Purpose: Provides the version of the application or service. This can be helpful for debugging or ensuring proper deployments.
    ///// Use Case: Enables operators to quickly verify the deployed version.
    ///// </summary>
    ///// <returns></returns>
    //public IActionResult Version()
    //{
    //    //TODO: Implement
    //    /*
    //    {
    //      "version": "1.2.3",
    //      "build": "abc123",
    //      "commit": "def456"
    //    }
    //     */

    //    throw new NotImplementedException();
    //}

    ///// <summary>
    ///// Purpose: Lists all critical dependencies and their statuses.
    ///// Use Case: Provides a higher-level overview of the service’s environment and dependency health.
    ///// </summary>
    ///// <returns></returns>
    //public IActionResult Dependencies()
    //{
    //    //TODO: Implement

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