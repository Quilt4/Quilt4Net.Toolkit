using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Features.Health;

internal class HealthClient : IHealthClient
{
    private readonly HealthClientOptions _clientOptions;

    //NOTE: The options needs to be default, if not used, this will fail at startup if not registered.
    public HealthClient(IOptions<HealthClientOptions> options)
    {
        _clientOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken = default)
    {
        if (_clientOptions.HealthAddress == null) throw new ArgumentNullException(nameof(HealthClientOptions.HealthAddress), $"No {nameof(HealthClientOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_clientOptions.HealthAddress);
        using var result = await client.GetAsync("live", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<LiveResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_clientOptions.HealthAddress == null) throw new ArgumentNullException(nameof(HealthClientOptions.HealthAddress), $"No {nameof(HealthClientOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_clientOptions.HealthAddress);
        using var result = await client.GetAsync("ready", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<ReadyResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_clientOptions.HealthAddress == null) throw new ArgumentNullException(nameof(HealthClientOptions.HealthAddress), $"No {nameof(HealthClientOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_clientOptions.HealthAddress);
        using var result = await client.GetAsync("health", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<HealthResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_clientOptions.HealthAddress == null) throw new ArgumentNullException(nameof(HealthClientOptions.HealthAddress), $"No {nameof(HealthClientOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_clientOptions.HealthAddress);
        using var result = await client.GetAsync("metrics", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<MetricsResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (_clientOptions.HealthAddress == null) throw new ArgumentNullException(nameof(HealthClientOptions.HealthAddress), $"No {nameof(HealthClientOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_clientOptions.HealthAddress);
        using var result = await client.GetAsync("version", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<VersionResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }
}