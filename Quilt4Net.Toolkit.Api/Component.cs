namespace Quilt4Net.Toolkit.Api;

public record Component
{
    public required string Name { get; init; }
    public required bool Essential { get; init; }
    public required Func<IServiceProvider, Task<CheckResult>> CheckAsync { get; init; }
}