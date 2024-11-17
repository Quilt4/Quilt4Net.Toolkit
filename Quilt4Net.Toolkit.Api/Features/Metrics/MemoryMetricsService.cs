using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

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
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[1], out var value) ? value : 0; // Extract value in KB
    }
}