namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal class RemoteConfigurationService : IRemoteConfigurationService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public RemoteConfigurationService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null)
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl);
    }
}