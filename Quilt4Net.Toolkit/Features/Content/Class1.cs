using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Features.FeatureToggle;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;

namespace Quilt4Net.Toolkit.Blazor;

public interface ILanguageService
{
    IAsyncEnumerable<Language> GetLanguagesAsync();
}

public interface IContentService
{
    Task<string> GetContentAsync(string key, string defaultValue);
    Task SetContentAsync(string key, string value);
}

internal class ContentService : IContentService
{
    private readonly IRemoteContentCallService _remoteContentCallService;

    public ContentService(IRemoteContentCallService remoteContentCallService)
    {
        _remoteContentCallService = remoteContentCallService;
    }

    public Task<string> GetContentAsync(string key, string defaultValue)
    {
        return _remoteContentCallService.GetContentAsync(key, defaultValue);
    }

    public Task SetContentAsync(string key, string value)
    {
        return _remoteContentCallService.SetContentAsync(key, value);
    }
}

public interface IRemoteContentCallService
{
    Task<string> GetContentAsync(string key, string defaultValue);
    Task SetContentAsync(string key, string defaultValue);
}

internal class RemoteContentCallService : IRemoteContentCallService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EnvironmentName _environmentName;
    private readonly ILogger<RemoteContentCallService> _logger;
    private readonly Quilt4NetServerOptions _options;
    private readonly ConcurrentDictionary<string, GetContentResponse> _localCache = new();

    public RemoteContentCallService(IServiceProvider serviceProvider, EnvironmentName environmentName, IOptions<Quilt4NetServerOptions> options, ILogger<RemoteContentCallService> logger)
    {
        _serviceProvider = serviceProvider;
        _environmentName = environmentName;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetContentAsync(string key, string defaultValue)
    {
        defaultValue ??= $"No content for '{key}'.";
        var ttl = _options.Ttl;

        try
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var request = new GetContentRequest
            {
                Key = key,
                Language = null, //TODO: null is default, use "selected" language here.
                Application = assemblyName?.Name,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                //Version = $"{assemblyName?.Version}",
                DefaultValue = $"{defaultValue}",
                //ValueType = typeof(T).Name,
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
                var address = $"Api/Content/{complexKey}";
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

                result = await response.Content.ReadFromJsonAsync<GetContentResponse>();

                _localCache.AddOrUpdate(key, result, (a, b) => result);
            }

            return result.Value ?? defaultValue;
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, "{Message} Key {Key}.", e.Message, key);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using fallback value '{Fallback}' for key {Key}.", e.Message, defaultValue, key);
            //_logger.LogError(e, "{Message} Using fallback value '{Fallback}' for key {Key}", e.Message, defaultValue, key);
            return defaultValue;
        }
    }

    public async Task SetContentAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value), $"No {nameof(value)} provided for key '{key}'.");

        var ttl = _options.Ttl;

        try
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var setContentRequest = new SetContentRequest
            {
                Key = key,
                Language = null, //TODO: null is default, use "selected" language here.
                Application = assemblyName?.Name,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                //Version = $"{assemblyName?.Version}",
                Value = $"{value}",
                //ValueType = typeof(T).Name,
                //Ttl = ttl
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", _options.ApiKey);
            client.BaseAddress = new Uri(_options.Address);
            var address = $"Api/Content";
            var response = await client.PostAsJsonAsync(address, setContentRequest);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            Console.WriteLine(e);
            Debugger.Break();
            throw;
        }
    }

    private static string BuildKey(GetContentRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);
        return payload;
    }
}

public record Language
{
    public required string Name { get; init; }
}

internal class LanguageService : ILanguageService
{
    public async IAsyncEnumerable<Language> GetLanguagesAsync()
    {
        yield return new Language { Name = "Default" };
    }
}