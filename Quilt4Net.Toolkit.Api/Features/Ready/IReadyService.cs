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
    Task<ReadyResponse> GetStatusAsync(CancellationToken cancellationToken);
}