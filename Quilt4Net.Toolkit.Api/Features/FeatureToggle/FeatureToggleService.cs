using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

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
        return await MakeCallAsync(key, fallback);
    }

    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default)
    {
        return await MakeCallAsync(key, fallback);
    }

    private async Task<T> MakeCallAsync<T>(string key, T fallback)
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
        var request = new FeatureToggleRequest
        {
            Key = key,
            Environment = _hostEnvironment.EnvironmentName,
            Application = assemblyName?.Name,
            Version = $"{assemblyName?.Version}",
            Instance = null, //TODO: Make this configurable
            FallbackValue = $"{fallback}",
            ValueType = typeof(T).Name
        };
        var payload = BuildKey<T>(request);

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

    private static string BuildKey<T>(FeatureToggleRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);
        return payload;
    }
}