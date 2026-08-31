namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal interface IRemoteConfigCallService
{
    Task<T> MakeCallAsync<T>(string key, T defaultValue, TimeSpan? ttl, string application = null);

    /// <summary>
    /// As <see cref="MakeCallAsync{T}"/>, but also reports where the value came from, so a caller
    /// can tell a fallback apart from a server value that happens to equal it.
    /// </summary>
    Task<ConfigurationResult<T>> MakeCallResultAsync<T>(string key, T defaultValue, TimeSpan? ttl, string application = null);
    Task<ConfigurationResponse[]> GetAllAsync();
    Task DeleteAsync(string key, string application, string environment, string instance);
    Task SetValueAsync(string key, string application, string environment, string instance, string value);
}