namespace Quilt4Net.Toolkit.Api.Features.Live;

public interface ILiveService
{
    ValueTask<LiveResponse> GetStatusAsync();
}