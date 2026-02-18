namespace Quilt4Net.Toolkit.Health.Features.Heartbeat;

public interface IHeartbeatService
{
    public Task ExecuteAsync(CancellationToken cancellationToken);
}