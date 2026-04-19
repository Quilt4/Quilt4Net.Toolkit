namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal class RemoteConfigurationService : IRemoteConfigurationService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public RemoteConfigurationService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    [Obsolete($"Use {nameof(GetAsync)} instead.")]
    public ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = null)
    {
        return GetAsync(key, fallback, ttl, application);
    }

    public async ValueTask<T> GetAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = null)
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl, application);
    }

    public ValueTask<bool> GetToggleAsync(string key, bool fallback = default, TimeSpan? ttl = null, string application = null)
    {
        return GetAsync(key, fallback, ttl, application);
    }

    [Obsolete($"Use {nameof(GetAsync)} instead.")]
    public Task<ConfigurationResponse[]> GetTogglesAsync()
    {
        return GetAsync();
    }

    public Task<ConfigurationResponse[]> GetAsync()
    {
        return _remoteConfigCallService.GetAllAsync();
    }

    public Task DeleteAsync(string key, string application, string environment, string instance)
    {
        return _remoteConfigCallService.DeleteAsync(key, application, environment, instance);
    }

    [Obsolete($"Use {nameof(SetAsync)} instead.")]
    public Task SetValueAsync(string key, string application, string environment, string instance, string value)
    {
        return SetAsync(key, application, environment, instance, value);
    }

    public Task SetAsync(string key, string application, string environment, string instance, string value)
    {
        return _remoteConfigCallService.SetValueAsync(key, application, environment, instance, value);
    }
}