using System.Net.Http.Json;
using System.Text.Json;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Health;
using Quilt4Net.Toolkit.Live;
using Quilt4Net.Toolkit.Ready;

namespace Quilt4Net.Toolkit.Client;

internal class HealthClieht : IHealthClieht
{
    private readonly Quilt4NetOptions _options;

    public HealthClieht(Quilt4NetOptions options)
    {
        _options = options;
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