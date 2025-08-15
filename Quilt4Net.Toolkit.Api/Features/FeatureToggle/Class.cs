using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Text;

namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public interface IFeatureToggleService
{
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false);
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default);
}

internal class FeatureToggleService : IFeatureToggleService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Quilt4NetApiOptions _options;

    public FeatureToggleService(IHostEnvironment hostEnvironment, IOptions<Quilt4NetApiOptions> options)
    {
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public async ValueTask<bool> GetToggleAsync(string key, bool fallback = false)
    {
        return await MakeCallAsync<bool>(key);
    }

    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default)
    {
        return await MakeCallAsync<T>(key);
    }

    private async Task<T> MakeCallAsync<T>(string key)
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
        var request = new FeatureToggleRequest
        {
            Key = key,
            Environment = _hostEnvironment.EnvironmentName,
            ApplicationName = assemblyName?.Name,
            ApplicationVersion = $"{assemblyName?.Version}"
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);

        //TODO: Handle cache

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", _options.FeatureToggle.ApiKey);
        client.BaseAddress = new Uri(_options.FeatureToggle.Address);
        var response = await client.GetAsync($"Api/FeatureToggle/{payload}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FeatureToggleResponse>();

        var value = (T)Convert.ChangeType(result.Value, typeof(T));
        return value;
    }
}

public record FeatureToggleRequest
{
    public required string Key { get; init; }
    public required string Environment { get; init; }
    public required string ApplicationName { get; init; }
    public required string ApplicationVersion { get; init; }
}

public record FeatureToggleResponse
{
    public required string Value { get; init; }
}