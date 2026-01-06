namespace Quilt4Net.Toolkit.Framework;

public interface IFeatureToggleService
{
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null);
}