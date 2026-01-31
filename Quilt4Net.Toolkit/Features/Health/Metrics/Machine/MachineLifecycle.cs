namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

public record MachineLifecycle
{
    public required DateTime BootTime { get; init; }
    public required TimeSpan Uptime { get; init; }
}