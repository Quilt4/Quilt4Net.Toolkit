using System.Diagnostics;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

internal class MetricsService : IMetricsService
{
    private readonly IMemoryMetricsService _memoryMetricsService;
    private readonly IProcessorMetricsService _processorMetricsService;

    public MetricsService(IMemoryMetricsService memoryMetricsService, IProcessorMetricsService processorMetricsService)
    {
        _memoryMetricsService = memoryMetricsService;
        _processorMetricsService = processorMetricsService;
    }

    public Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();

        var applicationUpTime = DateTime.Now - process.StartTime;
        var memory = _memoryMetricsService.GetMemory(process);
        var processor = _processorMetricsService.GetProcessor(process);

        var metrics = new MetricsResponse
        {
            ApplicationUptime = applicationUpTime,
            Memory = memory,
            Processor = processor
        };

        return Task.FromResult(metrics);
    }
}