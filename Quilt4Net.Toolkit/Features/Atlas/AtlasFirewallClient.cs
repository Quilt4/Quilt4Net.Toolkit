using System.Net;
using System.Net.Http.Json;
using Quilt4Net.Toolkit.Features.ValueGroup;

namespace Quilt4Net.Toolkit.Features.Atlas;

/// <summary>
/// <see cref="IAtlasFirewallClient"/> bound to one <see cref="AtlasFirewallProxyKeyEntry"/>. Sends the
/// entry's API key as <c>X-API-KEY</c> per request (the key is per-entry, not per-client) and the
/// entry's <c>GroupId</c> in each body. The base address is the Quilt4Net server (configured at
/// registration).
/// </summary>
internal sealed class AtlasFirewallClient : IAtlasFirewallClient
{
    private readonly HttpClient _httpClient;
    private readonly AtlasFirewallProxyKeyEntry _entry;

    public AtlasFirewallClient(HttpClient httpClient, AtlasFirewallProxyKeyEntry entry)
    {
        _httpClient = httpClient;
        _entry = entry;
    }

    public async Task<FirewallOpenResult> OpenAsync(string ip, string name = null, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        return await PostAsync<FirewallOpenResult>("Api/AtlasFirewall/open", new { groupId = _entry.GroupId, ip, name }, cancellationToken);
    }

    public async Task<FirewallCloseResult> CloseAsync(string ip, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        return await PostAsync<FirewallCloseResult>("Api/AtlasFirewall/close", new { groupId = _entry.GroupId, ip }, cancellationToken);
    }

    public Task<FirewallUsageResult> ReportUsedAsync(string ip, CancellationToken cancellationToken = default)
        => PostAsync<FirewallUsageResult>("Api/AtlasFirewall/used", new { groupId = _entry.GroupId, ip }, cancellationToken);

    public async Task<FirewallStateResult> GetStateAsync(string ip, CancellationToken cancellationToken = default)
    {
        var uri = $"Api/AtlasFirewall/state?groupId={Uri.EscapeDataString(_entry.GroupId ?? string.Empty)}&ip={Uri.EscapeDataString(ip ?? string.Empty)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-API-KEY", _entry.ApiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await ThrowIfUnauthorizedOrFailed(response);
        return await response.Content.ReadFromJsonAsync<FirewallStateResult>(cancellationToken);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-API-KEY", _entry.ApiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await ThrowIfUnauthorizedOrFailed(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private void EnsureManage()
    {
        if (!_entry.CanManage)
            throw new InvalidOperationException(
                "This firewall key is usage-only (firewall:usage) and cannot open or close the firewall. Use a manage key, or call ReportUsedAsync.");
    }

    private static async Task ThrowIfUnauthorizedOrFailed(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AtlasFirewallAuthorizationException(
                $"Server returned {(int)response.StatusCode} {response.StatusCode} for a firewall call. The key may be revoked, lack the required firewall scope, or target a group it is not bound to.");
        response.EnsureSuccessStatusCode();
    }
}
