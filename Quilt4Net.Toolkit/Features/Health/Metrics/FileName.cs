using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Health;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

/// <summary>
/// Service for Metrics.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Get metrics information.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Service for Merics that provides memory information.
/// </summary>
public interface IMemoryMetricsService
{
    /// <summary>
    /// Get memory information for a process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Memory GetMemory(Process process);
}

/// <summary>
/// Service for Merics that provides processor information.
/// </summary>
public interface IProcessorMetricsService
{
    /// <summary>
    /// Get processor information for a process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Processor GetProcessor(Process process);
}

internal class MemoryMetricsService : IMemoryMetricsService
{
    private readonly ILogger<MemoryMetricsService> _logger;

    public MemoryMetricsService(ILogger<MemoryMetricsService> logger)
    {
        _logger = logger;
    }

    public Memory GetMemory(Process process)
    {
        var applicationMemoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0);

        var memory = GetMemory();

        return new Memory
        {
            ApplicationMemoryUsageMb = applicationMemoryUsageMb,
            AvailableFreeMemoryMb = memory.FreeMemoryMB,
            TotalMemoryMb = memory.TotalMemoryMB
        };
    }

    private (double TotalMemoryMB, double FreeMemoryMB) GetMemory()
    {
        double totalMemoryMb = 0;
        double freeMemoryMb = 0;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                (totalMemoryMb, freeMemoryMb) = GetWindowsMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                (totalMemoryMb, freeMemoryMb) = GetLinuxMemory();
            }
            else
            {
                throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
            }
        }
        catch (Exception e)
        {
            _logger?.LogWarning(e, e.Message);
        }

        return (totalMemoryMb, freeMemoryMb);
    }

    private static (double TotalMemoryMB, double FreeMemoryMB) GetWindowsMemory()
    {
        using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
        foreach (var obj in searcher.Get())
        {
            var totalMemoryKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
            var freeMemoryKb = Convert.ToDouble(obj["FreePhysicalMemory"]);
            return (totalMemoryKb / 1024.0, freeMemoryKb / 1024.0); // Convert KB to MB
        }
        throw new InvalidOperationException("Unable to retrieve memory information on Windows.");
    }

    private static (double TotalMemoryMB, double FreeMemoryMB) GetLinuxMemory()
    {
        const string memInfoPath = "/proc/meminfo";
        if (!File.Exists(memInfoPath))
        {
            throw new InvalidOperationException("/proc/meminfo not found on Linux.");
        }

        double totalMemoryKb = 0;
        double freeMemoryKb = 0;

        foreach (var line in File.ReadLines(memInfoPath))
        {
            if (line.StartsWith("MemTotal:"))
            {
                totalMemoryKb = ParseMemInfoLine(line);
            }
            else if (line.StartsWith("MemAvailable:"))
            {
                freeMemoryKb = ParseMemInfoLine(line);
                break; // We found the key lines we need
            }
        }

        if (totalMemoryKb == 0 || freeMemoryKb == 0)
        {
            throw new InvalidOperationException("Unable to retrieve memory information from /proc/meminfo.");
        }

        return (totalMemoryKb / 1024.0, freeMemoryKb / 1024.0); // Convert KB to MB
    }

    private static double ParseMemInfoLine(string line)
    {
        var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[1], out var value) ? value : 0; // Extract value in KB
    }
}

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

internal class ProcessorMetricsService : IProcessorMetricsService
{
    private readonly ILogger<ProcessorMetricsService> _logger;

    public ProcessorMetricsService(ILogger<ProcessorMetricsService> logger)
    {
        _logger = logger;
    }

    public Processor GetProcessor(Process process)
    {
        var cpuTime = process.TotalProcessorTime;
        var numberOfCores = Environment.ProcessorCount;
        var processorSpeedGHz = GetProcessorSpeedGHz();

        // Calculate Total GHz-Hours
        var totalGHzHours = (cpuTime.TotalHours * processorSpeedGHz * numberOfCores);

        return new Processor
        {
            CpuTime = cpuTime,
            TotalGHzHours = totalGHzHours,
            NumberOfCores = numberOfCores,
            ProcessorSpeedGHz = processorSpeedGHz,
        };
    }

    private double GetProcessorSpeedGHz()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetProcessorSpeedWindows();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetProcessorSpeedLinux();
            }
        }
        catch (Exception e)
        {
            _logger?.LogWarning(e, e.Message);
        }

        return 0;
    }

    static double GetProcessorSpeedWindows()
    {
        using var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
        foreach (var obj in searcher.Get())
        {
            var clockSpeedMHz = Convert.ToDouble(obj["MaxClockSpeed"]); // MHz
            return clockSpeedMHz / 1000; // Convert to GHz
        }
        throw new InvalidOperationException("Unable to determine processor speed on Windows.");
    }

    static double GetProcessorSpeedLinux()
    {
        const string cpuInfoPath = "/proc/cpuinfo";
        if (!File.Exists(cpuInfoPath))
        {
            throw new InvalidOperationException("/proc/cpuinfo not found on Linux.");
        }

        foreach (var line in File.ReadLines(cpuInfoPath))
        {
            if (line.StartsWith("cpu MHz"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out var speedMHz))
                {
                    return speedMHz / 1000; // Convert to GHz
                }
            }
        }

        throw new InvalidOperationException("Unable to determine processor speed on Linux.");
    }
}