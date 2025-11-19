using System.Diagnostics;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Features.Health.Metrics;

namespace Quilt4Net.Toolkit.Sample;

public class BackgroundHealthCheckService
{
    private readonly IHealthService _healthService;
    private readonly IMetricsService _metricsService;

    public BackgroundHealthCheckService(IHealthService healthService, IMetricsService metricsService)
    {
        _healthService = healthService;
        _metricsService = metricsService;
    }

    public async Task Heartbeat()
    {
        var health = await _healthService.GetStatusAsync().ToArrayAsync();
        var metrics = await _metricsService.GetMetricsAsync();

        Debugger.Break();
    }
}