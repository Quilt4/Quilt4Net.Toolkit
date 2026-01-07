using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

internal class ProcessorMetricsService : IProcessorMetricsService
{
    private readonly ILogger<ProcessorMetricsService> _logger;

    private static int? _cachedPhysicalCores;
    private static double? _cachedMaxClockGhz;
    private static double? _cachedL3CacheMb;

    public ProcessorMetricsService(ILogger<ProcessorMetricsService> logger)
    {
        _logger = logger;
    }

    public Processor GetProcessor(Process process)
    {
        var cpuTime = process.TotalProcessorTime;

        var logicalCores = Environment.ProcessorCount;
        var physicalCores = GetPhysicalCpuCores();
        var maxClockGhz = GetMaxCpuSpeedGHz();
        var currentClockGhz = GetCurrentCpuSpeedGHz();
        var l3CacheMb = GetL3CacheMb();

        var effectiveClock = maxClockGhz ?? currentClockGhz;
        var totalGhzHours = cpuTime.TotalHours * effectiveClock * logicalCores;

        return new Processor
        {
            CpuTime = cpuTime,
            TotalGHzHours = totalGhzHours,
            NumberOfCores = logicalCores,
            PhysicalCpuCores = physicalCores,
            ProcessorSpeedGHz = effectiveClock,
            CurrentCpuSpeedGHz = currentClockGhz,
            L3CacheMb = l3CacheMb
        };
    }

    private int? GetPhysicalCpuCores()
    {
        if (_cachedPhysicalCores != null)
        {
            return _cachedPhysicalCores;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher =
                    new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");

                var cores = 0;
                foreach (var obj in searcher.Get())
                {
                    cores += Convert.ToInt32(obj["NumberOfCores"]);
                }

                _cachedPhysicalCores = cores > 0 ? cores : null;
                return _cachedPhysicalCores;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var output = Execute("lscpu", "-p=core");
                var cores = output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.StartsWith("#"))
                    .Distinct()
                    .Count();

                _cachedPhysicalCores = cores > 0 ? cores : null;
                return _cachedPhysicalCores;
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return null;
    }

    private double? GetMaxCpuSpeedGHz()
    {
        if (_cachedMaxClockGhz != null)
        {
            return _cachedMaxClockGhz;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher =
                    new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");

                foreach (var obj in searcher.Get())
                {
                    _cachedMaxClockGhz = Convert.ToDouble(obj["MaxClockSpeed"]) / 1000;
                    return _cachedMaxClockGhz;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var output = Execute("lscpu", "");
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("CPU max MHz"))
                    {
                        _cachedMaxClockGhz = double.Parse(line.Split(':')[1].Trim()) / 1000;
                        return _cachedMaxClockGhz;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return null;
    }

    private double? GetCurrentCpuSpeedGHz()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher =
                    new ManagementObjectSearcher("SELECT CurrentClockSpeed FROM Win32_Processor");

                foreach (var obj in searcher.Get())
                {
                    return Convert.ToDouble(obj["CurrentClockSpeed"]) / 1000;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                foreach (var line in File.ReadLines("/proc/cpuinfo"))
                {
                    if (line.StartsWith("cpu MHz"))
                    {
                        return double.Parse(line.Split(':')[1].Trim()) / 1000;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return null;
    }

    private double? GetL3CacheMb()
    {
        if (_cachedL3CacheMb != null)
        {
            return _cachedL3CacheMb;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher =
                    new ManagementObjectSearcher("SELECT L3CacheSize FROM Win32_Processor");

                foreach (var obj in searcher.Get())
                {
                    var kb = Convert.ToDouble(obj["L3CacheSize"]);
                    _cachedL3CacheMb = kb > 0 ? kb / 1024 : null;
                    return _cachedL3CacheMb;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var path = "/sys/devices/system/cpu/cpu0/cache/index3/size";
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path).Trim().ToUpperInvariant();
                    if (text.EndsWith("K"))
                    {
                        _cachedL3CacheMb = double.Parse(text.TrimEnd('K')) / 1024;
                        return _cachedL3CacheMb;
                    }

                    if (text.EndsWith("M"))
                    {
                        _cachedL3CacheMb = double.Parse(text.TrimEnd('M'));
                        return _cachedL3CacheMb;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return null;
    }

    private static string Execute(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}