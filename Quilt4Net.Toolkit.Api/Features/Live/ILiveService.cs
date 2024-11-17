namespace Quilt4Net.Toolkit.Api.Features.Live;

/// <summary>
/// Service for Live.
/// </summary>
public interface ILiveService
{
    /// <summary>
    /// Performs Live checks.
    /// This method always returns Alive.
    /// </summary>
    /// <returns></returns>
    ValueTask<LiveResponse> GetStatusAsync();
}