namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal interface IRemoteConfigCallService
{
    Task<T> MakeCallAsync<T>(string key, T defaultValue, TimeSpan? ttl, string application = null);
    Task<ConfigurationResponse[]> GetAllAsync();
    Task DeleteAsync(string key, string application, string environment, string instance);
    Task SetValueAsync(string key, string application, string environment, string instance, string value);
}