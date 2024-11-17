namespace Quilt4Net.Toolkit.Api.Features.Ready;

public interface IReadyService
{
    Task<ReadyResponse> GetStatusAsync(CancellationToken cancellationToken);
}