namespace Quilt4Net.Toolkit.Features.Health.Metrics.Storage;

public record Storage
{
    public required IReadOnlyCollection<StorageDevice> Devices { get; init; }
}