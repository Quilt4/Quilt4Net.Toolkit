using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

internal class MemoryMetricsService : IMemoryMetricsService
{
    private readonly ILogger<MemoryMetricsService> _logger;

    private static double? _cachedTotalMemoryGb;

    public MemoryMetricsService(ILogger<MemoryMetricsService> logger)
    {
        _logger = logger;
    }

    public Memory GetMemory(Process process)
    {
        var applicationMemoryUsageGb = process.WorkingSet64 / 1024.0 / 1024.0 / 1024.0;

        var (totalGb, freeGb) = GetMemory();

        return new Memory
        {
            ApplicationMemoryUsageGb = applicationMemoryUsageGb,
            TotalMemoryGb = totalGb,
            AvailableFreeMemoryGb = freeGb
        };
    }

    private (double? totalGb, double? freeGb) GetMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemory();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemory();
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return (null, null);
    }

    private static (double? totalGb, double? freeGb) GetWindowsMemory()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

        foreach (var obj in searcher.Get())
        {
            var totalKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
            var freeKb = Convert.ToDouble(obj["FreePhysicalMemory"]);

            if (_cachedTotalMemoryGb == null)
            {
                _cachedTotalMemoryGb = totalKb / 1024.0 / 1024.0;
            }

            var freeGb = freeKb / 1024.0 / 1024.0;

            return (_cachedTotalMemoryGb, freeGb);
        }

        return (null, null);
    }

    private static (double? totalGb, double? freeGb) GetLinuxMemory()
    {
        const string path = "/proc/meminfo";
        if (!File.Exists(path))
        {
            return (null, null);
        }

        double totalKb = 0;
        double freeKb = 0;

        foreach (var line in File.ReadLines(path))
        {
            if (line.StartsWith("MemTotal:"))
            {
                totalKb = ParseKb(line);
            }
            else if (line.StartsWith("MemAvailable:"))
            {
                freeKb = ParseKb(line);
            }

            if (totalKb > 0 && freeKb > 0)
            {
                break;
            }
        }

        if (totalKb == 0 || freeKb == 0)
        {
            return (null, null);
        }

        if (_cachedTotalMemoryGb == null)
        {
            _cachedTotalMemoryGb = totalKb / 1024.0 / 1024.0;
        }

        var freeGb = freeKb / 1024.0 / 1024.0;

        return (_cachedTotalMemoryGb, freeGb);
    }

    private static double ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return double.TryParse(parts[1], out var value) ? value : 0;
    }
}