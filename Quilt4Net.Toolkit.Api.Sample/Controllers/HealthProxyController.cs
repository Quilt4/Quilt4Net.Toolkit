using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Sample;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class HeartbeatController : ControllerBase
{
    private readonly Class1 _class1;

    public HeartbeatController(Class1 class1)
    {
        _class1 = class1;
    }
    [HttpGet]
    public async Task<IActionResult> Send()
    {
        await _class1.Heartbeat();
        return Ok();
    }
}

/// <summary>
/// Proxy controller for health checks.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthProxyController : ControllerBase
{
    private readonly IHealthClient _healthClient;

    public HealthProxyController(IHealthClient healthClient)
    {
        _healthClient = healthClient;
    }

    /// <summary>
    /// Live proxy.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("live-proxy")]
    public async Task<IActionResult> GetLive(CancellationToken cancellationToken)
    {
        var result = await _healthClient.GetLiveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Route("ready-proxy")]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        var result = await _healthClient.GetReadyAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Route("health-proxy")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthClient.GetHealthAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Route("metrics-proxy")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await _healthClient.GetMetricsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Route("version-proxy")]
    public async Task<IActionResult> GetVersion(CancellationToken cancellationToken)
    {
        var result = await _healthClient.GetVersionAsync(cancellationToken);
        return Ok(result);
    }
}