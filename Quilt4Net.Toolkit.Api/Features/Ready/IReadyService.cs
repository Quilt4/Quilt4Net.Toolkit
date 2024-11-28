using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

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
    //Task<ReadyResponse> GetStatusAsync(CancellationToken cancellationToken);
}