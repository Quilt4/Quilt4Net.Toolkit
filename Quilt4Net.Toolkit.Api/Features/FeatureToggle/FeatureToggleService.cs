using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;

namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

internal class FeatureToggleService : IFeatureToggleService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;
    private readonly Quilt4NetApiOptions _options;
    private readonly ILogger<FeatureToggleService> _logger;
    private readonly ConcurrentDictionary<string, FeatureToggleResponse> _localCache = new();

    public FeatureToggleService(IHostEnvironment hostEnvironment, IServiceProvider serviceProvider, IOptions<Quilt4NetApiOptions> options, ILogger<FeatureToggleService> logger)
    {
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null)
    {
        return await MakeCallAsync(key, fallback, ttl);
    }

    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null)
    {
        return await MakeCallAsync(key, fallback, ttl);
    }

    private async Task<T> MakeCallAsync<T>(string key, T defaultValue, TimeSpan? ttl)
    {
        ttl ??= _options.FeatureToggle.Ttl;

        try
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var request = new FeatureToggleRequest
            {
                Key = key,
                Application = assemblyName?.Name,
                Environment = _hostEnvironment.EnvironmentName,
                Instance = _options.FeatureToggle.InstanceLoader?.Invoke(_serviceProvider),
                Version = $"{assemblyName?.Version}",
                DefaultValue = $"{defaultValue}",
                ValueType = typeof(T).Name,
                Ttl = ttl
            };
            var payload = BuildKey<T>(request);

            var needRefresh = true;
            if (_localCache.TryGetValue(key, out var result))
            {
                needRefresh = DateTime.UtcNow > result.ValidTo;
            }

            if (needRefresh)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-API-KEY", _options.FeatureToggle.ApiKey);
                client.BaseAddress = new Uri(_options.FeatureToggle.Address);
                var address = $"Api/FeatureToggle/{payload}";
                var response = await client.GetAsync(address);
                //var address = $"Api/FeatureToggle/{key}/{request.Application}/{request.Environment}/{request.Instance ?? "-"}/{request.Version}";
                //var response = await client.GetAsync(address);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Unable to get feature toggle for key {Key} (Application: {Application}, Environment: {Environment}) from '{Address}' Response was {StatusCode} {ReasonPhrase}. Using fallback value '{Fallback}'.",
                        key, request.Application, request.Environment, address, response.StatusCode, response.ReasonPhrase, defaultValue);
                    return defaultValue;
                }

                result = await response.Content.ReadFromJsonAsync<FeatureToggleResponse>();

                _localCache.AddOrUpdate(key, result, (a, b) => result);
            }

            var value = (T)Convert.ChangeType(result.Value, typeof(T));
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using fallback value '{Fallback}' for key {Key}", e.Message, defaultValue, key);
            return defaultValue;
        }
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