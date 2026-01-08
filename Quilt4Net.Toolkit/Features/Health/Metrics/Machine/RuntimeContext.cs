namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

public record RuntimeContext
{
    public required string CurrentUser { get; init; }
    public required string ProcessArchitecture { get; init; } // x86 / x64 / arm64
    public bool IsElevated { get; init; }
}