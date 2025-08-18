namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IFeatureToggleService
{
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null);
}