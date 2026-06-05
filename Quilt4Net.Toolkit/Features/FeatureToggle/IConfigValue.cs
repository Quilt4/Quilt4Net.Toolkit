namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public interface IConfigValue
{
    public string Key { get; }
    public string Application { get; }
    public string Environment { get; }
    public string Instance { get; }
    public string Value { get; }

    /// <summary>
    /// Effective cache lifetime — how long consumers may serve a cached value before re-checking
    /// the server. Surfaced so admin UIs can warn that a change won't take effect until the
    /// existing cached value expires. <c>null</c> means "no TTL set; consumer defaults apply".
    /// </summary>
    public TimeSpan? Ttl { get; }
}