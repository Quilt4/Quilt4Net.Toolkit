namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IRemoteConfigurationService
{
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null);
}