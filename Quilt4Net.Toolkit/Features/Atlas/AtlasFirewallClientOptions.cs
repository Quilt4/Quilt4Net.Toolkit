namespace Quilt4Net.Toolkit.Features.Atlas;

/// <summary>
/// Options for the Atlas firewall proxy client. Only the Quilt4Net server address is needed — the
/// per-call API key and group come from the <c>AtlasFirewallProxyKeyEntry</c> in the value-group bundle.
/// </summary>
public class AtlasFirewallClientOptions
{
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
