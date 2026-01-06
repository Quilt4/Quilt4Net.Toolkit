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

    public Task<ConfigurationResponse[]> GetTogglesAsync()
    {
        return _remoteConfigCallService.GetAllAsync();
    }

    public Task DeleteAsync(string key, string application, string environment, string instance)
    {
        return _remoteConfigCallService.DeleteAsync(key, application, environment, instance);
    }

    public Task SetValueAsync(string key, string application, string environment, string instance, string value)
    {
        return _remoteConfigCallService.SetValueAsync(key, application, environment, instance, value);
    }
}