//using System.Diagnostics;
//using System.Management;
//using System.Runtime.InteropServices;
//using Microsoft.Extensions.Logging;
//using Quilt4Net.Toolkit.Features.Health;

//namespace Quilt4Net.Toolkit.Api.Features.Metrics;

//internal class ProcessorMetricsService : IProcessorMetricsService
//{
//    private readonly ILogger<ProcessorMetricsService> _logger;

//    public ProcessorMetricsService(ILogger<ProcessorMetricsService> logger)
//    {
//        _logger = logger;
//    }

//    public Processor GetProcessor(Process process)
//    {
//        var cpuTime = process.TotalProcessorTime;
//        var numberOfCores = Environment.ProcessorCount;
//        var processorSpeedGHz = GetProcessorSpeedGHz();

//        // Calculate Total GHz-Hours
//        var totalGHzHours = (cpuTime.TotalHours * processorSpeedGHz * numberOfCores);

//        return new Processor
//        {
//            CpuTime = cpuTime,
//            TotalGHzHours = totalGHzHours,
//            NumberOfCores = numberOfCores,
//            ProcessorSpeedGHz = processorSpeedGHz,
//        };
//    }

//    private double GetProcessorSpeedGHz()
//    {
//        try
//        {
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                return GetProcessorSpeedWindows();
//            }

//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//            {
//                return GetProcessorSpeedLinux();
//            }
//        }
//        catch (Exception e)
//        {
//            _logger?.LogWarning(e, e.Message);
//        }

//        return 0;
//    }

//    static double GetProcessorSpeedWindows()
//    {
//        using var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
//        foreach (var obj in searcher.Get())
//        {
//            var clockSpeedMHz = Convert.ToDouble(obj["MaxClockSpeed"]); // MHz
//            return clockSpeedMHz / 1000; // Convert to GHz
//        }
//        throw new InvalidOperationException("Unable to determine processor speed on Windows.");
//    }

//    static double GetProcessorSpeedLinux()
//    {
//        const string cpuInfoPath = "/proc/cpuinfo";
//        if (!File.Exists(cpuInfoPath))
//        {
//            throw new InvalidOperationException("/proc/cpuinfo not found on Linux.");
//        }

//        foreach (var line in File.ReadLines(cpuInfoPath))
//        {
//            if (line.StartsWith("cpu MHz"))
//            {
//                var parts = line.Split(':');
//                if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out var speedMHz))
//                {
//                    return speedMHz / 1000; // Convert to GHz
//                }
//            }
//        }

//        throw new InvalidOperationException("Unable to determine processor speed on Linux.");
//    }
//}