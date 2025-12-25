using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Features.FeatureToggle;

internal class RemoteConfigCallService : IRemoteConfigCallService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EnvironmentName _environmentName;
    private readonly RemoteConfigurationOptions _options;
    private readonly ILogger<RemoteConfigCallService> _logger;
    private readonly ConcurrentDictionary<string, FeatureToggleResponse> _localCache = new();

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
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-API-KEY", _options.ApiKey);
                client.BaseAddress = new Uri(_options.Quilt4NetAddress);
                var address = $"Api/Configuration/{complexKey}";
                var response = await client.GetAsync(address);
                //var address = $"Api/FeatureToggle/{key}/{request.Application}/{request.Environment}/{request.Instance ?? "-"}/{request.Version}";
                //var response = await client.GetAsync(address);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException($"Unable to get feature toggle for key '{key}' from address '{address}'. Response was '{response.StatusCode} {response.ReasonPhrase}'.");

                    _logger.LogError("Unable to get feature toggle for key '{Key}' (Application: {Application}, Environment: {Environment}) from '{HealthAddress}' Response was {StatusCode} {ReasonPhrase}. Using fallback value '{Fallback}'.",
                        key, request.Application, request.Environment, address, response.StatusCode, response.ReasonPhrase, defaultValue);
                    return defaultValue;
                }

                result = await response.Content.ReadFromJsonAsync<FeatureToggleResponse>();

                _localCache.AddOrUpdate(key, result, (a, b) => result);
            }

            if (result.Value == null) return defaultValue;
            var value = (T)Convert.ChangeType(result.Value, typeof(T));
            return value;
        }
        catch (InvalidCastException e)
        {
            _logger.LogError(e, "{Message} Value of type {type} is not supported for {key}.", e.Message, typeof(T).Name, key);
            throw new Exception($"Value of type {typeof(T).Name} is not supported.", e);
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, "{Message} Key {Key}.", e.Message, key);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using fallback value '{Fallback}' for key {Key}.", e.Message, defaultValue, key);
            return defaultValue;
        }
    }

    private static string BuildKey(FeatureToggleRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);
        return payload;
    }
}