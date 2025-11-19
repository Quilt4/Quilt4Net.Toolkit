namespace Quilt4Net.Toolkit.Features.Health.Dependency;

public interface IDependencyService
{
    IAsyncEnumerable<KeyValuePair<string, DependencyComponent>> GetStatusAsync(CancellationToken cancellationToken);
}