using Microsoft.Extensions.Logging;
using System.Management;
using System.Runtime.InteropServices;

namespace Quilt4Net.Toolkit.Features.Health.Metrics.Storage;

internal class StorageMetricsService : IStorageMetricsService
{
    private readonly ILogger<StorageMetricsService> _logger;

    public StorageMetricsService(ILogger<StorageMetricsService> logger)
    {
        _logger = logger;
    }

    public Storage GetStorage()
    {
        try
        {
            var devices = new List<StorageDevice>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                devices.Add(new StorageDevice
                {
                    Name = drive.Name,
                    MountPoint = drive.RootDirectory.FullName,
                    Type = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? GetWindowsStorageType(drive)
                        : MapDriveType(drive),
                    FileSystem = drive.DriveFormat,
                    TotalSizeGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0,
                    AvailableSizeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0,
                    IsReadOnly = drive.DriveType == DriveType.CDRom
                });
            }

            return new Storage
            {
                Devices = devices
            };
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, e.Message);
            return new Storage
            {
                Devices = Array.Empty<StorageDevice>()
            };
        }
    }

    private static StorageDeviceType MapDriveType(DriveInfo drive)
    {
        if (drive.DriveType == DriveType.Network)
        {
            return StorageDeviceType.Network;
        }

        if (drive.DriveType == DriveType.Removable)
        {
            return StorageDeviceType.Removable;
        }

        return StorageDeviceType.Local;
    }

    static StorageDeviceType GetWindowsStorageType(DriveInfo drive)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '{drive.Name.TrimEnd('\\')}'");

        foreach (var disk in searcher.Get())
        {
            var providerName = disk["ProviderName"]?.ToString();
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                return StorageDeviceType.Network;
            }

            var volumeName = disk["VolumeName"]?.ToString() ?? string.Empty;
            var fileSystem = disk["FileSystem"]?.ToString() ?? string.Empty;

            if (fileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase) &&
                volumeName.Contains("google", StringComparison.OrdinalIgnoreCase))
            {
                return StorageDeviceType.Virtual;
            }
        }

        return drive.DriveType switch
        {
            DriveType.Network => StorageDeviceType.Network,
            DriveType.Removable => StorageDeviceType.Removable,
            _ => StorageDeviceType.Local
        };
    }
}
