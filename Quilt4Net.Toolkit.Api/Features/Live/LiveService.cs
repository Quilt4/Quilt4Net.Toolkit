namespace Quilt4Net.Toolkit.Api.Features.Live;

internal class LiveService : ILiveService
{
    public ValueTask<LiveResponse> GetStatusAsync()
    {
        return ValueTask.FromResult(new LiveResponse { Status = LiveStatus.Alive });
    }
}