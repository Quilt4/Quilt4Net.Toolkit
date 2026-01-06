namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IRemoteConfigurationService
{
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null);
    Task<ConfigurationResponse[]> GetTogglesAsync();
    Task DeleteAsync(string key, string application, string environment, string instance);
    Task SetValueAsync(string key, string application, string environment, string instance, string value);
}