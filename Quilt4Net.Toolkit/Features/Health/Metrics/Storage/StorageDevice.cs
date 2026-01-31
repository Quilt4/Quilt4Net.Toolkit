namespace Quilt4Net.Toolkit.Features.Health.Metrics.Storage;

public record StorageDevice
{
    public required string Name { get; init; }
    public required string MountPoint { get; init; }
    public required StorageDeviceType Type { get; init; }

    public string? FileSystem { get; init; }

    public required double TotalSizeGb { get; init; }
    public required double AvailableSizeGb { get; init; }

    public required bool IsReadOnly { get; init; }
}