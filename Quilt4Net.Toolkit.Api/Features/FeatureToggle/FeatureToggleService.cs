namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

internal class FeatureToggleService : IFeatureToggleService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public FeatureToggleService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    public async ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null)
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl);
    }
}