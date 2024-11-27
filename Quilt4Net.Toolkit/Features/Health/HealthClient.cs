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

    public async Task<LiveResponse> GetLiveAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HealthAddress == null) throw new ArgumentNullException(nameof(Quilt4NetHealthOptions.HealthAddress), $"No {nameof(Quilt4NetHealthOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        using var result = await client.GetAsync("live", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<LiveResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<ReadyResponse> GetReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HealthAddress == null) throw new ArgumentNullException(nameof(Quilt4NetHealthOptions.HealthAddress), $"No {nameof(Quilt4NetHealthOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        using var result = await client.GetAsync("ready", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<ReadyResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HealthAddress == null) throw new ArgumentNullException(nameof(Quilt4NetHealthOptions.HealthAddress), $"No {nameof(Quilt4NetHealthOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        using var result = await client.GetAsync("health", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<HealthResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<MetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HealthAddress == null) throw new ArgumentNullException(nameof(Quilt4NetHealthOptions.HealthAddress), $"No {nameof(Quilt4NetHealthOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        using var result = await client.GetAsync("metrics", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<MetricsResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }

    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HealthAddress == null) throw new ArgumentNullException(nameof(Quilt4NetHealthOptions.HealthAddress), $"No {nameof(Quilt4NetHealthOptions.HealthAddress)} configured.");

        using var client = new HttpClient();
        client.BaseAddress = _options.HealthAddress;
        using var result = await client.GetAsync("version", cancellationToken);
        var content = await result.Content.ReadFromJsonAsync<VersionResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return content;
    }
}