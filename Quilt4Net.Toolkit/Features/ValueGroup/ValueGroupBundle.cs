using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Typed bundle returned by <see cref="IValueGroupClient.GetAsync"/>. New member types
/// (KV pairs, Atlas credentials, …) get added as additional properties when later features ship —
/// the shape is additive-only so older clients keep deserializing successfully against newer servers.
/// </summary>
public record ValueGroupBundle
{
    public string GroupId { get; init; }
    public string GroupName { get; init; }
    public DateTime FetchedAtUtc { get; init; }
    public ConfigurationResponse[] FeatureToggles { get; init; } = [];
    public ApplicationInsightsConfigurationResponse[] ApplicationInsightsConfigurations { get; init; } = [];
    public KeyValueEntry[] KeyValues { get; init; } = [];
    public AtlasDatabaseAccessEntry[] AtlasDatabaseAccesses { get; init; } = [];
    public AtlasFirewallCredentialEntry[] AtlasFirewallCredentials { get; init; } = [];
    public AtlasFirewallProxyKeyEntry[] AtlasFirewallProxyKeys { get; init; } = [];
}
