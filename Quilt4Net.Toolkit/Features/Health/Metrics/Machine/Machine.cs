namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

public record Machine
{
    public required MachineIdentity Identity { get; init; }
    public required OperatingSystemInfo OperatingSystem { get; init; }
    public required RuntimeContext Runtime { get; init; }
    public required MachineLifecycle Lifecycle { get; init; }
}