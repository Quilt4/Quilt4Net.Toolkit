using System.Diagnostics;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

internal class MetricsService : IMetricsService
{
    private readonly IMemoryMetricsService _memoryMetricsService;
    private readonly IProcessorMetricsService _processorMetricsService;
    private readonly IGpuMetricsService _gpuMetricsService;
    private readonly IMachineMetricsService _machineMetricsService;
    private readonly IStorageMetricsService _storageMetricsService;

    public MetricsService(IMemoryMetricsService memoryMetricsService, IProcessorMetricsService processorMetricsService, IGpuMetricsService gpuMetricsService, IMachineMetricsService machineMetricsService, IStorageMetricsService storageMetricsService)
    {
        _memoryMetricsService = memoryMetricsService;
        _processorMetricsService = processorMetricsService;
        _gpuMetricsService = gpuMetricsService;
        _machineMetricsService = machineMetricsService;
        _storageMetricsService = storageMetricsService;
    }

    public Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        var process = Process.GetCurrentProcess();

        var applicationUpTime = DateTime.Now - process.StartTime;
        var machine = _machineMetricsService.GetMachine();
        var memory = _memoryMetricsService.GetMemory(process);
        var processor = _processorMetricsService.GetProcessor(process);
        var storage = _storageMetricsService.GetStorage();
        var gpu = _gpuMetricsService.GetGpu();

        sw.Stop();

        var metrics = new MetricsResponse
        {
            ApplicationUptime = applicationUpTime,
            Machine = machine,
            Memory = memory,
            Processor = processor,
            Storage = storage,
            Gpu = gpu,
            Elapsed = sw.Elapsed,
        };

        return Task.FromResult(metrics);
    }
}