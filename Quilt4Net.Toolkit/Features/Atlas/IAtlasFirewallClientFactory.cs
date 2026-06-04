using Quilt4Net.Toolkit.Features.ValueGroup;

namespace Quilt4Net.Toolkit.Features.Atlas;

/// <summary>
/// Creates an <see cref="IAtlasFirewallClient"/> for a specific firewall key. The key (and group) are
/// per-entry — typically obtained from a <see cref="ValueGroupBundle"/> — so clients are created on
/// demand rather than registered as a singleton with a fixed key.
/// </summary>
public interface IAtlasFirewallClientFactory
{
    IAtlasFirewallClient Create(AtlasFirewallProxyKeyEntry entry);
}
