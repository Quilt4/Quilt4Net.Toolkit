using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

internal class GpuMetricsService : IGpuMetricsService
{
    private readonly ILogger<GpuMetricsService> _logger;
    private static Gpu _cachedGpu;

    public GpuMetricsService(ILogger<GpuMetricsService> logger)
    {
        _logger = logger;
    }

    public Gpu GetGpu()
    {
        if (_cachedGpu != null)
        {
            return _cachedGpu;
        }

        try
        {
            if (TryGetNvidiaGpu(out var nvidiaGpu))
            {
                _cachedGpu = nvidiaGpu;
                return _cachedGpu;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (TryGetGpuWindowsWmi(out var windowsGpu))
                {
                    _cachedGpu = windowsGpu;
                    return _cachedGpu;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (TryGetGpuLinuxGeneric(out var linuxGpu))
                {
                    _cachedGpu = linuxGpu;
                    return _cachedGpu;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
        }

        return null;
    }

    private static bool TryGetNvidiaGpu(out Gpu gpu)
    {
        gpu = null;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name,memory.total,clocks.max.sm --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var line = process.StandardOutput.ReadLine();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var parts = line.Split(',');
            gpu = new Gpu
            {
                Name = parts[0].Trim(),
                VideoMemoryGb = double.Parse(parts[1].Trim()) / 1024,
                CoreClockGHz = double.Parse(parts[2].Trim()) / 1000
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetGpuWindowsWmi(out Gpu gpu)
    {
        gpu = null;

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, AdapterRAM FROM Win32_VideoController");

        foreach (var obj in searcher.Get())
        {
            var name = obj["Name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var ramBytes = Convert.ToDouble(obj["AdapterRAM"] ?? 0);

            gpu = new Gpu
            {
                Name = name,
                VideoMemoryGb = ramBytes > 0
                    ? ramBytes / 1024 / 1024 / 1024
                    : null,
                CoreClockGHz = null
            };

            return true;
        }

        return false;
    }

    private static bool TryGetGpuLinuxGeneric(out Gpu gpu)
    {
        gpu = null;

        try
        {
            var name = Execute("lspci", "-mm | grep -i 'vga\\|3d'");
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            double? vramGb = null;
            var vramPath = "/sys/class/drm/card0/device/mem_info_vram_total";

            if (File.Exists(vramPath))
            {
                var bytes = double.Parse(File.ReadAllText(vramPath).Trim());
                vramGb = bytes / 1024 / 1024 / 1024;
            }

            gpu = new Gpu
            {
                Name = name.Trim(),
                VideoMemoryGb = vramGb,
                CoreClockGHz = null
            };

            return true;
        }
        catch
        {
            return false;
        }
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
