using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthProxyController : ControllerBase
{
    private readonly IHealthClient _healthClient;

    public HealthProxyController(IHealthClient healthClient)
    {
        _healthClient = healthClient;
    }

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