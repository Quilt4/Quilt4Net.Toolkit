using Quilt4Net.Toolkit.Features.ValueGroup;

namespace Quilt4Net.Toolkit.Features.Atlas;

internal sealed class AtlasFirewallClientFactory : IAtlasFirewallClientFactory
{
    /// <summary>Named <see cref="IHttpClientFactory"/> client (BaseAddress + timeout set at registration).</summary>
    public const string HttpClientName = "Quilt4Net.AtlasFirewall";

    private readonly IHttpClientFactory _httpClientFactory;

    public AtlasFirewallClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IAtlasFirewallClient Create(AtlasFirewallProxyKeyEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.ApiKey))
            throw new ArgumentException("AtlasFirewallProxyKeyEntry.ApiKey is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.GroupId))
            throw new ArgumentException("AtlasFirewallProxyKeyEntry.GroupId is required.", nameof(entry));

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        return new AtlasFirewallClient(httpClient, entry);
    }
}
