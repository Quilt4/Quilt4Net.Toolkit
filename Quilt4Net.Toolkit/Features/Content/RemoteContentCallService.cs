using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

internal class RemoteContentCallService : IRemoteContentCallService
{
    private readonly EnvironmentName _environmentName;
    private readonly ILogger<RemoteContentCallService> _logger;
    private readonly Quilt4NetServerOptions _options;
    private readonly ConcurrentDictionary<string, GetContentResponse> _localCache = new();

    public RemoteContentCallService(EnvironmentName environmentName, IOptions<Quilt4NetServerOptions> options, ILogger<RemoteContentCallService> logger)
    {
        _environmentName = environmentName;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetContentAsync(string key, string defaultValue, ContentFormat? contentType)
    {
        if (string.IsNullOrEmpty(_options.ApiKey)) return "No ApiKey provided.";

        defaultValue ??= $"No content for '{key}'.";

        try
        {
            string selectedLanguage = null; //TODO: null is default, use "selected" language here.

            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var request = new GetContentRequest
            {
                Key = key,
                Language = selectedLanguage,
                Application = assemblyName?.Name,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                DefaultValue = $"{defaultValue}",
                ContentFormat = contentType
            };
            var complexKey = BuildKey(request);

            var needRefresh = true;
            if (_localCache.TryGetValue($"{key}_{selectedLanguage}", out var result))
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

                _localCache.AddOrUpdate($"{key}_{selectedLanguage}", result, (_, _) => result);
            }

            return result.Value ?? defaultValue;
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, "{Message} Key {Key}.", e.Message, key);
            throw;
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "{Message} Status code {StatusCode}. Using fallback value '{Fallback}' for key {Key}.", e.Message, e.StatusCode, defaultValue, key);
            return defaultValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using fallback value '{Fallback}' for key {Key}.", e.Message, defaultValue, key);
            return defaultValue;
        }
    }

    public async Task SetContentAsync(string key, string value, ContentFormat contentType)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value), $"No {nameof(value)} provided for key '{key}'.");

        try
        {
            string selectedLanguage = null; //TODO: null is default, use "selected" language here.

            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var setContentRequest = new SetContentRequest
            {
                Key = key,
                Language = selectedLanguage,
                Application = assemblyName?.Name,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                Value = $"{value}",
                ContentType = contentType
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", _options.ApiKey);
            client.BaseAddress = new Uri(_options.Address);
            var address = "Api/Content";
            var response = await client.PostAsJsonAsync(address, setContentRequest);
            response.EnsureSuccessStatusCode();

            _localCache.TryRemove($"{key}_{selectedLanguage}", out _);
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