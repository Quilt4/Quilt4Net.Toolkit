namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal class RemoteConfigurationService : IRemoteConfigurationService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public RemoteConfigurationService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    /// <inheritdoc />
    public async ValueTask<T> GetAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = "")
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl, application);
    }

    /// <inheritdoc />
    public ValueTask<bool> GetToggleAsync(string key, bool fallback = default, TimeSpan? ttl = null, string application = "")
    {
        return GetAsync(key, fallback, ttl, application);
    }

    /// <inheritdoc />
    public Task<ConfigurationResponse[]> GetAsync()
    {
        return _remoteConfigCallService.GetAllAsync();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, string application, string environment, string instance)
    {
        return _remoteConfigCallService.DeleteAsync(key, application, environment, instance);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string application, string environment, string instance, string value)
    {
        return _remoteConfigCallService.SetValueAsync(key, application, environment, instance, value);
    }
}