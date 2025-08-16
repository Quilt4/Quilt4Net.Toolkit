namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public interface IFeatureToggleService
{
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null);
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null);
}