using Quilt4Net.Toolkit.Api.Features.Health;

namespace Quilt4Net.Toolkit.Api;

public record Component
{
    public required string Name { get; init; }
    public required Func<IServiceProvider, Task<HealthStatusResult>> CheckAsync { get; init; }
}