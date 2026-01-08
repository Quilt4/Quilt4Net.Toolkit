using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

public record Machine
{
    public required MachineIdentity Identity { get; init; }
    public required OperatingSystemInfo OperatingSystem { get; init; }
    public required RuntimeContext Runtime { get; init; }
    public required MachineLifecycle Lifecycle { get; init; }
}

public record MachineIdentity
{
    public required string MachineName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
    public string Manufacturer { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Model { get; init; }

    public required MachineEnvironmentType EnvironmentType { get; init; }
}

public record OperatingSystemInfo
{
    public required string Platform { get; init; }           // Windows / Linux

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Distribution { get; init; }                // Ubuntu, RHEL, etc.

    public required string Version { get; init; }
    public required int AddressSizeBits { get; init; }        // 32 / 64
}

public record RuntimeContext
{
    public required string CurrentUser { get; init; }
    public required string ProcessArchitecture { get; init; } // x86 / x64 / arm64
    public bool IsElevated { get; init; }
}

public record MachineLifecycle
{
    public required DateTime BootTime { get; init; }
    public required TimeSpan Uptime { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MachineEnvironmentType
{
    Physical,
    VirtualMachine,
    Container
}

public interface IMachineMetricsService
{
    Machine GetMachine();
}

internal class MachineMetricsService : IMachineMetricsService
{
    private readonly ILogger<MachineMetricsService> _logger;

    private static Machine _cachedMachine;

    public MachineMetricsService(ILogger<MachineMetricsService> logger)
    {
        _logger = logger;
    }

    public Machine GetMachine()
    {
        if (_cachedMachine != null)
        {
            return BuildWithUpdatedUptime(_cachedMachine);
        }

        try
        {
            var identity = GetIdentity();
            var os = GetOperatingSystem();
            var runtime = GetRuntimeContext();
            var lifecycle = GetLifecycle();

            _cachedMachine = new Machine
            {
                Identity = identity,
                OperatingSystem = os,
                Runtime = runtime,
                Lifecycle = lifecycle
            };

            return _cachedMachine;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
            return null;
        }
    }

    private static Machine BuildWithUpdatedUptime(Machine machine)
    {
        var uptime = DateTime.UtcNow - machine.Lifecycle.BootTime;

        return machine with
        {
            Lifecycle = machine.Lifecycle with
            {
                Uptime = uptime
            }
        };
    }

    private static MachineIdentity GetIdentity()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Model FROM Win32_ComputerSystem");

            foreach (var obj in searcher.Get())
            {
                return new MachineIdentity
                {
                    MachineName = Environment.MachineName,
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    Model = obj["Model"]?.ToString(),
                    EnvironmentType = DetectEnvironmentType()
                };
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var vendor = ReadFile("/sys/class/dmi/id/sys_vendor");
            var model = ReadFile("/sys/class/dmi/id/product_name");

            return new MachineIdentity
            {
                MachineName = Environment.MachineName,
                Manufacturer = vendor,
                Model = model,
                EnvironmentType = DetectEnvironmentType()
            };
        }

        throw new PlatformNotSupportedException();
    }

    private static OperatingSystemInfo GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new OperatingSystemInfo
            {
                Platform = "Windows",
                Distribution = null,
                Version = Environment.OSVersion.VersionString,
                AddressSizeBits = Environment.Is64BitOperatingSystem ? 64 : 32
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var distro = ReadOsReleaseValue("PRETTY_NAME");

            return new OperatingSystemInfo
            {
                Platform = "Linux",
                Distribution = distro,
                Version = RuntimeInformation.OSDescription,
                AddressSizeBits = Environment.Is64BitOperatingSystem ? 64 : 32
            };
        }

        throw new PlatformNotSupportedException();
    }

    private static RuntimeContext GetRuntimeContext()
    {
        return new RuntimeContext
        {
            CurrentUser = GetCurrentUserIdentifier(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            IsElevated = IsElevated()
        };
    }

    private static MachineLifecycle GetLifecycle()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT LastBootUpTime FROM Win32_OperatingSystem");

            foreach (var obj in searcher.Get())
            {
                var bootTime = ManagementDateTimeConverter
                    .ToDateTime(obj["LastBootUpTime"].ToString())
                    .ToUniversalTime();

                return new MachineLifecycle
                {
                    BootTime = bootTime,
                    Uptime = DateTime.UtcNow - bootTime
                };
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var uptimeSeconds = double.Parse(ReadFile("/proc/uptime")!.Split(' ')[0]);
            var bootTime = DateTime.UtcNow - TimeSpan.FromSeconds(uptimeSeconds);

            return new MachineLifecycle
            {
                BootTime = bootTime,
                Uptime = TimeSpan.FromSeconds(uptimeSeconds)
            };
        }

        throw new PlatformNotSupportedException();
    }

    private static MachineEnvironmentType DetectEnvironmentType()
    {
        if (File.Exists("/.dockerenv") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            return MachineEnvironmentType.Container;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Model FROM Win32_ComputerSystem");

            foreach (var obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString()?.ToLowerInvariant();
                if (model != null && (model.Contains("virtual") || model.Contains("vmware")))
                {
                    return MachineEnvironmentType.VirtualMachine;
                }
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var productName = ReadFile("/sys/class/dmi/id/product_name")?.ToLowerInvariant();
            if (productName != null &&
                (productName.Contains("kvm") ||
                 productName.Contains("vmware") ||
                 productName.Contains("virtual")))
            {
                return MachineEnvironmentType.VirtualMachine;
            }
        }

        return MachineEnvironmentType.Physical;
    }

    private static bool IsElevated()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return Environment.UserName == "root";
    }

    private static string ReadOsReleaseValue(string key)
    {
        const string path = "/etc/os-release";
        if (!File.Exists(path))
        {
            return null;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (line.StartsWith($"{key}="))
            {
                return line.Split('=')[1].Trim('"');
            }
        }

        return null;
    }

    private static string ReadFile(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    static string GetCurrentUserIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsUserIdentifier();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxUserIdentifier();
        }

        return Environment.UserName;
    }

    static string GetWindowsUserIdentifier()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();

            // Preferred: DOMAIN\User
            if (!string.IsNullOrWhiteSpace(identity.Name))
            {
                return identity.Name;
            }

            // Fallback: DOMAIN/User
            return $"{Environment.UserDomainName}\\{Environment.UserName}";
        }
        catch
        {
            return $"{Environment.UserDomainName}\\{Environment.UserName}";
        }
    }

    static string GetLinuxUserIdentifier()
    {
        try
        {
            var uid = Execute("id", "-u").Trim();
            var user = Environment.UserName;

            if (!string.IsNullOrWhiteSpace(user))
            {
                return user;
            }

            return $"uid:{uid}";
        }
        catch
        {
            return Environment.UserName;
        }
    }

    static string Execute(string fileName, string arguments)
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
