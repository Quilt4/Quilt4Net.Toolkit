using System.Diagnostics;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

internal class MetricsService : IMetricsService
{
    private readonly IMemoryMetricsService _memoryMetricsService;
    private readonly IProcessorMetricsService _processorMetricsService;
    private readonly IGpuMetricsService _gpuMetricsService;

    public MetricsService(IMemoryMetricsService memoryMetricsService, IProcessorMetricsService processorMetricsService, IGpuMetricsService gpuMetricsService)
    {
        _memoryMetricsService = memoryMetricsService;
        _processorMetricsService = processorMetricsService;
        _gpuMetricsService = gpuMetricsService;
    }

    public Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var process = Process.GetCurrentProcess();

        var applicationUpTime = DateTime.Now - process.StartTime;
        var memory = _memoryMetricsService.GetMemory(process);
        var processor = _processorMetricsService.GetProcessor(process);
        var gpu = _gpuMetricsService.GetGpu();

        var metrics = new MetricsResponse
        {
            ApplicationUptime = applicationUpTime,
            Memory = memory,
            Processor = processor,
            Gpu = gpu,
        };

        return Task.FromResult(metrics);
    }
}