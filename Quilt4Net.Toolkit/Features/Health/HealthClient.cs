using System.Net.Http.Json;
using System.Text.Json;

namespace Quilt4Net.Toolkit.Features.Health;

internal class HealthClient : IHealthClient
{
    private readonly Quilt4NetHealthOptions _options;

    //NOTE: The options needs to be default, if not used, this will fail at startup if not registered.
    public HealthClient(Quilt4NetHealthOptions options = default)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        var result = await client.GetFromJsonAsync<LiveResponse>("live", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result;
    }

    public async Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        var result = await client.GetFromJsonAsync<ReadyResponse>("ready", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        var result = await client.GetFromJsonAsync<HealthResponse>("health", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result;
    }

    public async Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        var result = await client.GetFromJsonAsync<MetricsResponse>("metrics", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result;
    }

    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        var result = await client.GetFromJsonAsync<VersionResponse>("version", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result;
    }
}