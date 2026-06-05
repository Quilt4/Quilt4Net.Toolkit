namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IRemoteConfigurationService
{
    ValueTask<T> GetAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = null);
    ValueTask<bool> GetToggleAsync(string key, bool fallback = default, TimeSpan? ttl = null, string application = null);
    Task<ConfigurationResponse[]> GetAsync();
    Task DeleteAsync(string key, string application, string environment, string instance);
    Task SetAsync(string key, string application, string environment, string instance, string value);
}