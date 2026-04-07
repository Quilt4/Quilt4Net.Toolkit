using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal class RemoteConfigCallService : IRemoteConfigCallService
{
    private static readonly TimeSpan FallbackFailureCacheDuration = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly EnvironmentName _environmentName;
    private readonly RemoteConfigurationOptions _options;
    private readonly ILogger<RemoteConfigCallService> _logger;
    private readonly ConcurrentDictionary<string, FeatureToggleResponse> _localCache = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _lastKnownTtl = new();

    public RemoteConfigCallService(IServiceProvider serviceProvider, EnvironmentName environmentName, IOptions<RemoteConfigurationOptions> options, ILogger<RemoteConfigCallService> logger)
    {
        _serviceProvider = serviceProvider;
        _environmentName = environmentName;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T> MakeCallAsync<T>(string key, T defaultValue, TimeSpan? ttl)
    {
        ttl ??= _options.Ttl;
        var sw = Stopwatch.StartNew();

        try
        {
            var changeType = ((T)Convert.ChangeType($"{defaultValue}", typeof(T)));
            if (!$"{changeType}".Equals($"{defaultValue}")) throw new NotSupportedException($"Value of type {typeof(T).Name} is not supported.");

            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var request = new FeatureToggleRequest
            {
                Key = key,
                Application = assemblyName?.Name,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                Version = $"{assemblyName?.Version}",
                DefaultValue = $"{defaultValue}",
                ValueType = typeof(T).Name,
                Ttl = ttl
            };
            var complexKey = BuildKey(request);

            var needRefresh = true;
            if (_localCache.TryGetValue(key, out var result))
            {
                needRefresh = DateTime.UtcNow > result.ValidTo;
            }

            if (needRefresh)
            {
                using var client = GetHttpClient();
                var address = $"Api/Configuration/{complexKey}";
                var response = await client.GetAsync(address);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Unable to get feature toggle for key '{Key}' (Application: {Application}, Environment: {Environment}) from '{HealthAddress}'. Response was {StatusCode} {ReasonPhrase}. Using stale cache or fallback.",
                        key, request.Application, request.Environment, address, response.StatusCode, response.ReasonPhrase);
                    CacheFailure(key, result);
                    var staleValue = GetCachedOrDefault(result, defaultValue);
                    _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: {Source}, Stale: true, Value: '{Value}'.",
                        key, sw.ElapsedMilliseconds, result != null ? "StaleCache" : "Default", staleValue);
                    return staleValue;
                }

                result = await response.Content.ReadFromJsonAsync<FeatureToggleResponse>();

                var interval = result.ValidTo - DateTime.UtcNow;
                if (interval > TimeSpan.Zero)
                    _lastKnownTtl[key] = interval;

                _localCache.AddOrUpdate(key, result, (a, b) => result);

                if (result.Value == null)
                {
                    _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: Server, Stale: false, Value: '{Value}'.",
                        key, sw.ElapsedMilliseconds, defaultValue);
                    return defaultValue;
                }

                var serverValue = (T)Convert.ChangeType(result.Value, typeof(T));
                _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: Server, Stale: false, Value: '{Value}'.",
                    key, sw.ElapsedMilliseconds, serverValue);
                return serverValue;
            }

            if (result.Value == null)
            {
                _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: Cache, Stale: false, Value: '{Value}'.",
                    key, sw.ElapsedMilliseconds, defaultValue);
                return defaultValue;
            }

            var cachedValue = (T)Convert.ChangeType(result.Value, typeof(T));
            _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: Cache, Stale: false, Value: '{Value}'.",
                key, sw.ElapsedMilliseconds, cachedValue);
            return cachedValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using stale cache or fallback for key {Key}.", e.Message, key);
            if (_localCache.TryGetValue(key, out var stale))
            {
                CacheFailure(key, stale);
                var staleValue = GetCachedOrDefault(stale, defaultValue);
                _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: StaleCache, Stale: true, Value: '{Value}'.",
                    key, sw.ElapsedMilliseconds, staleValue);
                return staleValue;
            }
            _logger.LogInformation("Configuration '{Key}' resolved in {Elapsed}ms. Source: Default, Stale: true, Value: '{Value}'.",
                key, sw.ElapsedMilliseconds, defaultValue);
            return defaultValue;
        }
    }

    public async Task<ConfigurationResponse[]> GetAllAsync()
    {
        try
        {
            using var client = GetHttpClient();
            var response = await client.GetAsync("Api/Configuration");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to get all configuration. Response was {StatusCode} {ReasonPhrase}. Returning empty list.",
                    response.StatusCode, response.ReasonPhrase);
                return [];
            }

            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var application = assemblyName?.Name;
            var environment = _environmentName.Name;

            var data = await response.Content.ReadFromJsonAsync<ConfigurationResponse[]>();
            return data.Where(x => x.Environment == environment && x.Application == application).ToArray();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Returning empty list.", e.Message);
            return [];
        }
    }

    public async Task DeleteAsync(string key, string application, string environment, string instance)
    {
        var requestBody = new
        {
            Key = key,
            Application = application,
            Environment = environment,
            Instance = instance,
        };

        using var client = GetHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Delete, "Api/Configuration");
        request.Content = JsonContent.Create(requestBody);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetValueAsync(string key, string application, string environment, string instance, string value)
    {
        var requestBody = new
        {
            Key = key,
            Application = application,
            Environment = environment,
            Instance = instance,
            Value = value
        };

        using var client = GetHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, "Api/Configuration");
        request.Content = JsonContent.Create(requestBody);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static T GetCachedOrDefault<T>(FeatureToggleResponse cached, T defaultValue)
    {
        if (cached?.Value == null) return defaultValue;
        try
        {
            return (T)Convert.ChangeType(cached.Value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    private void CacheFailure(string key, FeatureToggleResponse stale)
    {
        var duration = _lastKnownTtl.GetValueOrDefault(key, FallbackFailureCacheDuration);
        var failureResponse = new FeatureToggleResponse
        {
            Value = stale?.Value,
            ValidTo = DateTime.UtcNow.Add(duration)
        };
        _localCache.AddOrUpdate(key, failureResponse, (_, _) => failureResponse);
    }

    private static string BuildKey(FeatureToggleRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);
        return payload;
    }

    private HttpClient GetHttpClient()
    {
        HttpClient client = null;
        try
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", _options.ApiKey);
            client.BaseAddress = new Uri(_options.Quilt4NetAddress);
            return client;
        }
        catch
        {
            client?.Dispose();
            throw;
        }
    }
}