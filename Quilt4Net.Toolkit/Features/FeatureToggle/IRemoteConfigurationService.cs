namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IRemoteConfigurationService
{
    [Obsolete($"Use {nameof(GetAsync)} instead.")]
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = null);
    ValueTask<T> GetAsync<T>(string key, T fallback = default, TimeSpan? ttl = null, string application = null);
    ValueTask<bool> GetToggleAsync(string key, bool fallback = default, TimeSpan? ttl = null, string application = null);
    [Obsolete($"Use {nameof(GetAsync)} instead.")]
    Task<ConfigurationResponse[]> GetTogglesAsync();
    Task<ConfigurationResponse[]> GetAsync();
    Task DeleteAsync(string key, string application, string environment, string instance);
    [Obsolete($"Use {nameof(SetAsync)} instead.")]
    Task SetValueAsync(string key, string application, string environment, string instance, string value);
    Task SetAsync(string key, string application, string environment, string instance, string value);
}