namespace Quilt4Net.Toolkit.Features.Health.Ready;

/// <summary>
/// Service for Ready.
/// </summary>
public interface IReadyService
{
    /// <summary>
    /// Performs Ready checks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<KeyValuePair<string, ReadyComponent>> GetStatusAsync(CancellationToken cancellationToken);
}