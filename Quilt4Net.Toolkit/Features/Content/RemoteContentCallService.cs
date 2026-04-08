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
    private static readonly TimeSpan FallbackCacheDuration = TimeSpan.FromMinutes(10);

    private readonly EnvironmentName _environmentName;
    private readonly ContentOptions _contentOptions;
    private readonly ILogger<RemoteContentCallService> _logger;
    private readonly ConcurrentDictionary<string, GetContentResponse> _localCache = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _lastKnownTtl = new();
    private readonly ConcurrentDictionary<string, bool> _refreshInProgress = new();
    private Language[] _languages;
    private DateTime _languagesValidTo;
    private TimeSpan _lastKnownLanguageTtl;

    public RemoteContentCallService(EnvironmentName environmentName, IOptions<ContentOptions> contentOptions, ILogger<RemoteContentCallService> logger)
    {
        _environmentName = environmentName;
        _contentOptions = contentOptions.Value;
        _logger = logger;
    }

    public async Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null)
    {
        if (languageKey == Language.DeveloperLanguageKey) return ("X", true);

        defaultValue ??= $"No content for '{key}'.";

        if (languageKey == Language.NoApiKeyLanguageKey || string.IsNullOrEmpty(_contentOptions.ApiKey)) return (defaultValue, false);

        var sw = Stopwatch.StartNew();
        var cacheKey = $"{key}_{languageKey}";

        try
        {
            _localCache.TryGetValue(cacheKey, out var cached);
            var needRefresh = cached == null || DateTime.UtcNow > cached.ValidTo;

            if (!needRefresh)
            {
                _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: Cache, Stale: false.",
                    key, sw.ElapsedMilliseconds);
                return (cached.Value ?? defaultValue, true);
            }

            // Stale-while-revalidate: return stale value immediately, refresh in background.
            if (cached != null)
            {
                StartBackgroundRefresh(key, defaultValue, languageKey, contentType, application);
                _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: StaleCache, Stale: true.",
                    key, sw.ElapsedMilliseconds);
                return (cached.Value ?? defaultValue, true);
            }

            // No cache — must fetch with timeout.
            return await FetchContentWithTimeout(key, defaultValue, languageKey, contentType, sw, application);
        }
        catch (Exception e)
        {
            _localCache.TryGetValue(cacheKey, out var stale);
            var staleValue = stale?.Value ?? defaultValue;
            _logger.LogError(e, "{Message} Using stale cache or fallback for key {Key}.", e.Message, key);
            CacheFailure(key, languageKey, staleValue);
            _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: {Source}, Stale: true.",
                key, sw.ElapsedMilliseconds, stale != null ? "StaleCache" : "Default");
            return (staleValue, false);
        }
    }

    public async Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType, string application = null)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value), $"No {nameof(value)} provided for key '{key}'.");

        try
        {
            var assemblyName = application ?? _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;
            var setContentRequest = new SetContentRequest
            {
                Key = key,
                LanguageKey = languageKey,
                Application = assemblyName,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                Value = $"{value}",
                ContentType = contentType
            };

            using var client = GetHttpClient();
            var address = "Api/Content";
            var response = await client.PostAsJsonAsync(address, setContentRequest);
            response.EnsureSuccessStatusCode();

            _localCache.TryRemove($"{key}_{languageKey}", out _);

            //TODO: Notify the user that this content will be updated after this long time on all clients, because of cache.
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    public async Task<Language[]> GetLanguagesAsync(bool forceReload)
    {
        if (string.IsNullOrEmpty(_contentOptions.ApiKey)) return [new Language { Name = "No ApiKey provided.", Key = Language.NoApiKeyLanguageKey }];

        if (_languages != null && !forceReload && DateTime.UtcNow < _languagesValidTo) return _languages;

        try
        {
            var assemblyName = _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;

            using var client = GetHttpClient();
            var address = $"Api/Language/{assemblyName}/{_environmentName.Name}";
            var response = await client.GetAsync(address);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to get languages from '{Address}'. Response was {StatusCode} {ReasonPhrase}. Returning cached or empty list.",
                    address, response.StatusCode, response.ReasonPhrase);
                _languagesValidTo = DateTime.UtcNow.Add(_lastKnownLanguageTtl > TimeSpan.Zero ? _lastKnownLanguageTtl : FallbackCacheDuration);
                return _languages ?? [];
            }

            var result = await response.Content.ReadFromJsonAsync<LanguageResponse>();
            _languages = result.Languages;
            _languagesValidTo = result.ValidTo;

            var langInterval = result.ValidTo - DateTime.UtcNow;
            if (langInterval > TimeSpan.Zero)
                _lastKnownLanguageTtl = langInterval;

            return _languages;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Returning cached or empty list.", e.Message);
            _languagesValidTo = DateTime.UtcNow.Add(_lastKnownLanguageTtl > TimeSpan.Zero ? _lastKnownLanguageTtl : FallbackCacheDuration);
            return _languages ?? [];
        }
    }

    public async Task ClearContentCacheAsync()
    {
        _localCache.Clear();
    }

    private async Task<(string Value, bool Success)> FetchContentWithTimeout(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, Stopwatch sw, string application = null)
    {
        try
        {
            var assemblyName = application ?? _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;
            var request = new GetContentRequest
            {
                Key = key,
                LanguageKey = languageKey,
                Application = assemblyName,
                Environment = _environmentName.Name,
                Instance = null,
                DefaultValue = contentType == null ? null : $"{defaultValue}",
                ContentFormat = contentType
            };
            var complexKey = BuildKey(request);

            using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
            using var client = GetHttpClient();
            var address = $"Api/Content/{complexKey}";
            var response = await client.GetAsync(address, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unable to get content for key '{Key}'. Response was {StatusCode} {ReasonPhrase}.",
                    key, response.StatusCode, response.ReasonPhrase);
                CacheFailure(key, languageKey, defaultValue);
                _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: Default, Stale: true.",
                    key, sw.ElapsedMilliseconds);
                return (defaultValue, false);
            }

            var result = await response.Content.ReadFromJsonAsync<GetContentResponse>(cancellationToken: cts.Token);

            var cacheKey = $"{key}_{languageKey}";
            var interval = result.ValidTo - DateTime.UtcNow;
            if (interval > TimeSpan.Zero)
                _lastKnownTtl[cacheKey] = interval;

            _localCache.AddOrUpdate(cacheKey, result, (_, _) => result);

            _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: Server, Stale: false.",
                key, sw.ElapsedMilliseconds);
            return (result.Value ?? defaultValue, true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTTP request timed out for content '{Key}' after {Timeout}ms. Using default value.",
                key, _contentOptions.HttpTimeout.TotalMilliseconds);
            CacheFailure(key, languageKey, defaultValue);
            _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: Default, Stale: true.",
                key, sw.ElapsedMilliseconds);
            return (defaultValue, false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using default for content key {Key}.", e.Message, key);
            CacheFailure(key, languageKey, defaultValue);
            _logger.LogInformation("Content '{Key}' resolved in {Elapsed}ms. Source: Default, Stale: true.",
                key, sw.ElapsedMilliseconds);
            return (defaultValue, false);
        }
    }

    private void StartBackgroundRefresh(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null)
    {
        var cacheKey = $"{key}_{languageKey}";
        if (!_refreshInProgress.TryAdd(cacheKey, true)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var assemblyName = application ?? _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;
                var request = new GetContentRequest
                {
                    Key = key,
                    LanguageKey = languageKey,
                    Application = assemblyName,
                    Environment = _environmentName.Name,
                    Instance = null,
                    DefaultValue = contentType == null ? null : $"{defaultValue}",
                    ContentFormat = contentType
                };
                var complexKey = BuildKey(request);

                using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
                using var client = GetHttpClient();
                var address = $"Api/Content/{complexKey}";
                var response = await client.GetAsync(address, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Background refresh for content '{Key}' failed. Response was {StatusCode} {ReasonPhrase}.",
                        key, response.StatusCode, response.ReasonPhrase);
                    var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                    CacheFailure(key, languageKey, staleValue);
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<GetContentResponse>(cancellationToken: cts.Token);

                var interval = result.ValidTo - DateTime.UtcNow;
                if (interval > TimeSpan.Zero)
                    _lastKnownTtl[cacheKey] = interval;

                _localCache.AddOrUpdate(cacheKey, result, (_, _) => result);
                _logger.LogInformation("Background refresh for content '{Key}' completed. ValidTo: {ValidTo}.",
                    key, result.ValidTo);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Background refresh for content '{Key}' timed out after {Timeout}ms.",
                    key, _contentOptions.HttpTimeout.TotalMilliseconds);
                var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                CacheFailure(key, languageKey, staleValue);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Background refresh for content '{Key}' failed: {Message}.", key, e.Message);
                var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                CacheFailure(key, languageKey, staleValue);
            }
            finally
            {
                _refreshInProgress.TryRemove(cacheKey, out _);
            }
        });
    }

    private void CacheFailure(string key, Guid languageKey, string value)
    {
        var cacheKey = $"{key}_{languageKey}";
        var duration = _lastKnownTtl.GetValueOrDefault(cacheKey, _contentOptions.FailureCacheDuration);
        var failureResponse = new GetContentResponse
        {
            Value = value,
            ValidTo = DateTime.UtcNow.Add(duration)
        };
        _localCache.AddOrUpdate(cacheKey, failureResponse, (_, _) => failureResponse);
    }

    private static string BuildKey(GetContentRequest request)
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
            client.DefaultRequestHeaders.Add("X-API-KEY", _contentOptions.ApiKey);
            client.BaseAddress = new Uri(_contentOptions.Quilt4NetAddress);
            return client;
        }
        catch
        {
            client?.Dispose();
            throw;
        }
    }
}