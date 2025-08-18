using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;

namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public record EnvironmentName
{
    public string Name { get; init; }
}

internal interface IRemoteConfigCallService
{
    Task<T> MakeCallAsync<T>(string key, T defaultValue, TimeSpan? ttl);
}

public interface IFeatureToggleService
{
    ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null);
}

public interface IRemoteConfigurationService
{
    ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null);
}

internal class RemoteConfigCallService : IRemoteConfigCallService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EnvironmentName _environmentName;
    private readonly Quilt4NetServerOptions _options;
    private readonly ILogger<RemoteConfigCallService> _logger;
    private readonly ConcurrentDictionary<string, FeatureToggleResponse> _localCache = new();

    public RemoteConfigCallService(IServiceProvider serviceProvider, EnvironmentName environmentName, IOptions<Quilt4NetServerOptions> options, ILogger<RemoteConfigCallService> logger)
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
                Instance = _options.InstanceLoader?.Invoke(_serviceProvider),
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
                client.BaseAddress = new Uri(_options.Address);
                var address = $"Api/Configuration/{complexKey}";
                var response = await client.GetAsync(address);
                //var address = $"Api/FeatureToggle/{key}/{request.Application}/{request.Environment}/{request.Instance ?? "-"}/{request.Version}";
                //var response = await client.GetAsync(address);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException($"Unable to get feature toggle for key '{key}' from address '{address}'. Response was '{response.StatusCode} {response.ReasonPhrase}'.");

                    _logger.LogError("Unable to get feature toggle for key '{Key}' (Application: {Application}, Environment: {Environment}) from '{Address}' Response was {StatusCode} {ReasonPhrase}. Using fallback value '{Fallback}'.",
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

public record FeatureToggleRequest : IKeyContext
{
    public required string Key { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string Version { get; init; }
    public required string DefaultValue { get; init; }
    public required string ValueType { get; init; }
    public TimeSpan? Ttl { get; init; }
}

public record GetContentResponse
{
    public required string Value { get; init; }
    public required DateTime ValidTo { get; init; }
}

public record SetContentRequest : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required string Language { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string Value { get; init; }
}

public record GetContentRequest : ILanguageKeyContext
{
    public required string Key { get; init; }
    public required string Language { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string DefaultValue { get; init; }
    public TimeSpan? Ttl { get; init; }
}

public interface IKeyContext
{
    string Key { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
    string Version { get; }
}

public interface ILanguageKeyContext
{
    string Key { get; }
    string Language { get; }
    string Application { get; }
    string Environment { get; }
    string Instance { get; }
}

internal class FeatureToggleService : IFeatureToggleService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public FeatureToggleService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    public async ValueTask<bool> GetToggleAsync(string key, bool fallback = false, TimeSpan? ttl = null)
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl);
    }
}

internal class RemoteConfigurationService : IRemoteConfigurationService
{
    private readonly IRemoteConfigCallService _remoteConfigCallService;

    public RemoteConfigurationService(IRemoteConfigCallService remoteConfigCallService)
    {
        _remoteConfigCallService = remoteConfigCallService;
    }

    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default, TimeSpan? ttl = null)
    {
        return await _remoteConfigCallService.MakeCallAsync(key, fallback, ttl);
    }
}

public record FeatureToggleResponse
{
    /// <summary>
    /// Value of the feature toggle.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// After this time the client will automatically check for new values.
    /// </summary>
    public required DateTime ValidTo { get; init; }
}