using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ConcurrentDictionary<string, CachedContent> _localCache = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _lastKnownTtl = new();
    private readonly ConcurrentDictionary<string, bool> _refreshInProgress = new();
    private int _missingApiKeyWarned;
    private Language[] _languages;
    private DateTime _languagesValidTo;
    private TimeSpan _lastKnownLanguageTtl;

    /// <summary>Named <see cref="IHttpClientFactory"/> client for content calls to Quilt4Net.Server.</summary>
    public const string HttpClientName = "Quilt4Net.Content";

    private readonly IHttpClientFactory _httpClientFactory;

    public RemoteContentCallService(EnvironmentName environmentName, IOptions<ContentOptions> contentOptions, IHttpClientFactory httpClientFactory, ILogger<RemoteContentCallService> logger)
    {
        _environmentName = environmentName;
        _contentOptions = contentOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null)
    {
        var result = await GetContentResultAsync(key, defaultValue, languageKey, contentType, application, translations);
        return (result.Value, result.Success);
    }

    public async Task<ContentResult> GetContentResultAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null)
    {
        if (languageKey == Language.DeveloperLanguageKey) return Result("X", true, ContentSource.Developer, false);

        defaultValue ??= $"No content for '{key}'.";

        if (languageKey == Language.NoApiKeyLanguageKey || string.IsNullOrEmpty(_contentOptions.ApiKey))
        {
            WarnMissingApiKeyOnce();
            return Result(defaultValue, false, ContentSource.NoApiKey, true);
        }

        var sw = Stopwatch.StartNew();
        // Resolve effective application up front so cache key + request share the same value.
        // Convention: null -> lookup (options.Application or entry assembly name);
        // "" stays as "" (shared); value forwarded as-is.
        var effectiveApplication = ResolveApplication(application);
        var cacheKey = BuildCacheKey(key, languageKey, effectiveApplication);

        try
        {
            _localCache.TryGetValue(cacheKey, out var cached);
            var needRefresh = cached == null || DateTime.UtcNow > cached.ValidTo;

            // Per-resolution result lines are Debug — they fire on every content read (typically a
            // 0ms cache hit) and flooded Information traces in production. Errors/warnings still log
            // at their original levels. Matches RemoteConfigCallService.
            if (!needRefresh)
            {
                // A negative-cache entry holds the caller's default, not server content. Report it as
                // Default so an unseeded key is never mistaken for a genuine cache hit from the second
                // render onwards. Success stays true either way — unchanged from the legacy tuple.
                var source = cached.IsDefault ? ContentSource.Default : ContentSource.Cache;
                LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, source, stale: cached.IsDefault);
                return Result(cached.Value ?? defaultValue, true, source, cached.IsDefault, cached);
            }

            // Stale-while-revalidate: return stale value immediately, refresh in background.
            // Disabled via options → fall through to a synchronous fetch so the caller gets a fresh value.
            if (cached != null && _contentOptions.StaleWhileRevalidate)
            {
                StartBackgroundRefresh(key, cacheKey, defaultValue, languageKey, contentType, effectiveApplication, translations);
                var source = cached.IsDefault ? ContentSource.Default : ContentSource.StaleCache;
                LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, source, stale: true);
                return Result(cached.Value ?? defaultValue, true, source, true, cached);
            }

            // No cache (or stale-while-revalidate disabled) — fetch with timeout; the catch below
            // still falls back to any stale cached value on failure.
            return await FetchContentWithTimeout(key, cacheKey, defaultValue, languageKey, contentType, sw, effectiveApplication, translations);
        }
        catch (Exception e)
        {
            _localCache.TryGetValue(cacheKey, out var stale);
            var staleValue = stale?.Value ?? defaultValue;
            _logger.LogError(e, "{Message} Using stale cache or fallback for key {Key}.", e.Message, key);
            CacheFailure(cacheKey, staleValue);
            var source = stale is { IsDefault: false } ? ContentSource.StaleCache : ContentSource.Default;
            LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, source, stale: true);
            return Result(staleValue, false, source, true);
        }
    }

    private static ContentResult Result(string value, bool success, ContentSource source, bool stale, CachedContent metadata = null)
    {
        return new ContentResult
        {
            Value = value,
            Success = success,
            Source = source,
            Stale = stale,
            // Carried from the cache entry (or the fetch that just filled it) so a cache hit reports
            // the same fallback provenance as the server call did. Absent for the paths that never
            // reached the server — no API key, developer language, a hard failure — where "unknown"
            // is the honest answer rather than "no fallback".
            ServedLanguageKey = metadata?.ServedLanguageKey,
            FallbackReason = metadata?.FallbackReason ?? ContentFallbackReason.Unknown,
            IsStageFallback = metadata?.IsStageFallback ?? false,
        };
    }

    // Content was registered but cannot reach the server, so every value silently falls back to its
    // default. Warning rather than Error: nothing failed — the app renders correctly, and a key-less
    // setup is a legitimate local/dev state (hence Language.NoApiKeyLanguageKey). Logged once per
    // process: this is a startup misconfiguration, and the check runs on every single read.
    private void WarnMissingApiKeyOnce()
    {
        if (Interlocked.Exchange(ref _missingApiKeyWarned, 1) != 0) return;
        _logger.LogWarning(
            "No Quilt4Net content API key is configured. Every content value will fall back to its default and no lookups will be attempted. Set ContentOptions.ApiKey to enable content.");
    }

    public async Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType, string application = null)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value), $"No {nameof(value)} provided for key '{key}'.");

        try
        {
            var effectiveApplication = ResolveApplication(application);
            var setContentRequest = new SetContentRequest
            {
                Key = key,
                LanguageKey = languageKey,
                Application = effectiveApplication,
                Environment = _environmentName.Name,
                Instance = null, //_options.InstanceLoader?.Invoke(_serviceProvider),
                Value = $"{value}",
                ContentType = contentType
            };

            using var client = GetHttpClient();
            var address = "Api/Content";
            var response = await client.PostAsJsonAsync(address, setContentRequest);
            response.EnsureSuccessStatusCode();

            // This instance's cache is cleared immediately, but OTHER clients keep serving their
            // cached value until their own TTL expires (or StaleWhileRevalidate refreshes it) — so a
            // write is not instantly visible fleet-wide. Surface that as an informational hint.
            _localCache.TryRemove(BuildCacheKey(key, languageKey, effectiveApplication), out _);
            _logger.LogInformation(
                "Content '{Key}' updated. This client's cache was cleared; other clients will pick up the change after their cache TTL expires.",
                key);
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

        if (_languages != null && !forceReload && DateTime.UtcNow < _languagesValidTo)
        {
            // #132: trace cache-served language lists so a spinning selector can be told apart from
            // an actual server round-trip. Debug, opt-in via log config.
            _logger.LogDebug("Languages resolved from cache: {Count} language(s), valid to {ValidTo}.",
                _languages.Length, _languagesValidTo);
            return _languages;
        }

        var sw = Stopwatch.StartNew();
        var assemblyName = _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;
        var address = $"Api/Language/{assemblyName}/{_environmentName.Name}";

        try
        {
            using var client = GetHttpClient();
            var response = await client.GetAsync(address);

            WarnIfSlow(sw.ElapsedMilliseconds, $"language load via '{address}'", (int)response.StatusCode, response.ReasonPhrase);

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

            _logger.LogDebug("Languages loaded from '{Address}' in {Elapsed}ms: {Count} language(s), valid to {ValidTo}.",
                address, sw.ElapsedMilliseconds, _languages.Length, _languagesValidTo);

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

    public async Task WarmCacheAsync(Guid languageKey, string application = null)
    {
        if (string.IsNullOrEmpty(_contentOptions.ApiKey)) return;
        if (languageKey == Language.DeveloperLanguageKey || languageKey == Language.NoApiKeyLanguageKey) return;

        var effectiveApplication = ResolveApplication(application);
        if (string.IsNullOrEmpty(effectiveApplication)) return;

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
            using var client = GetHttpClient();
            var address = $"Api/Content/all/{Uri.EscapeDataString(effectiveApplication)}/{Uri.EscapeDataString(_environmentName.Name ?? "")}/{languageKey}";
            var response = await client.GetAsync(address, cts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Content warm-up endpoint unavailable (404) for application '{Application}' in environment '{Environment}', language '{LanguageKey}'. Server predates bulk content; falling back to per-key fetching.",
                    effectiveApplication, _environmentName.Name, languageKey);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Coordinates and body, because warm-up is per language: without them a host warming
                // two languages sees one failure line that cannot say which one dropped to per-key
                // fetching, and no reason at all (#155). The success line above has always named
                // them, so a failure naming nothing was the odd one out.
                var body = await ReadErrorBodyAsync(response, cts.Token);
                _logger.LogWarning("Content warm-up failed for application '{Application}' in environment '{Environment}', language '{LanguageKey}'. Response was {StatusCode} {ReasonPhrase}. {Body}Falling back to per-key fetching.",
                    effectiveApplication, _environmentName.Name, languageKey, response.StatusCode, response.ReasonPhrase, body);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<GetAllContentResponse>(cancellationToken: cts.Token);
            if (result?.Items == null) return;

            var ttl = result.ValidTo - DateTime.UtcNow;
            var kept = 0;
            foreach (var item in result.Items)
            {
                var cacheKey = BuildCacheKey(item.Key, languageKey, effectiveApplication);
                var cached = new CachedContent
                {
                    Value = item.Value,
                    ValidTo = result.ValidTo,
                    ServedLanguageKey = item.ServedLanguageKey,
                    FallbackReason = item.FallbackReason,
                    IsStageFallback = item.IsStageFallback,
                };
                _localCache.AddOrUpdate(cacheKey, cached, (_, existing) =>
                {
                    if (!ShouldKeepExisting(existing, cached)) return cached;
                    Interlocked.Increment(ref kept);
                    return existing;
                });
                if (ttl > TimeSpan.Zero) _lastKnownTtl[cacheKey] = ttl;
            }

            _logger.LogInformation("Content warm-up loaded {Count} item(s) in {Elapsed}ms for application '{Application}', language '{LanguageKey}'. ValidTo: {ValidTo}. Kept {Kept} newer cached value(s).",
                result.Items.Length, sw.ElapsedMilliseconds, effectiveApplication, languageKey, result.ValidTo, kept);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Content warm-up timed out after {Timeout}ms for application '{Application}' in environment '{Environment}', language '{LanguageKey}'. Falling back to per-key fetching.",
                _contentOptions.HttpTimeout.TotalMilliseconds, effectiveApplication, _environmentName.Name, languageKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Content warm-up failed for application '{Application}' in environment '{Environment}', language '{LanguageKey}': {Message} Falling back to per-key fetching.",
                effectiveApplication, _environmentName.Name, languageKey, e.Message);
        }
    }

    /// <summary>
    /// The error response body, trimmed to one log line, or an empty string when there is nothing
    /// useful to show. Bounded because the failing response is not always ours — a proxy or gateway
    /// in front of the server answers with a full HTML page, and that does not belong in a log.
    /// Never throws: this runs on a path that is already reporting a failure.
    /// </summary>
    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        const int maxLength = 200;
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;

            // One line: a problem-detail payload is multi-line JSON, and a log entry that wraps is
            // harder to scan than a truncated one.
            body = body.ReplaceLineEndings(" ").Trim();
            if (body.Length > maxLength) body = string.Concat(body.AsSpan(0, maxLength), "…");
            return $"Body: {body}. ";
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Whether a bulk warm-up result must leave an existing cache entry alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A bulk response is a <b>snapshot</b> taken when the request was issued, and warm-up is
    /// fire-and-forget: <c>LanguageStateService</c> starts it and raises
    /// <c>LanguageChangedEvent</c> in the same breath, so components take the per-key path
    /// concurrently. Without this guard the slower bulk response overwrites whatever those reads
    /// cached — including a value the server only produced <i>because</i> of them, such as a
    /// translation backfilled on first request. The user then sees the old language for the rest of
    /// the TTL, intermittently, depending on which response happened to land last.
    /// </para>
    /// <para>
    /// <see cref="CachedContent.ValidTo"/> doubles as a written-at stamp: the TTL is a server
    /// constant, so a later <c>ValidTo</c> means the entry was written later.
    /// </para>
    /// </remarks>
    private static bool ShouldKeepExisting(CachedContent existing, CachedContent incoming)
    {
        // A negative entry stands in for a value the server never confirmed, and its ValidTo comes
        // from FailureCacheDuration rather than a real response — so it can easily outlast a
        // genuine warm-up value. Real content always wins over it, timestamps notwithstanding.
        if (existing.IsDefault) return false;

        return existing.ValidTo > incoming.ValidTo;
    }

    public async Task WarmConfiguredLanguagesAsync(string application = null)
    {
        if (string.IsNullOrEmpty(_contentOptions.ApiKey)) return;

        // The default language (Guid.Empty) is always warmed — this is the pre-existing behaviour.
        await WarmCacheAsync(Guid.Empty, application);

        if (_contentOptions.WarmUpLanguages is not { Count: > 0 }) return;

        // Configured languages are named; resolve names -> keys against the server's language list.
        var languages = await GetLanguagesAsync(forceReload: false);
        var warmed = new HashSet<Guid> { Guid.Empty };
        foreach (var name in _contentOptions.WarmUpLanguages)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var language = languages.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
            if (language == null)
            {
                _logger.LogWarning("WarmUpLanguages: no language named '{Name}' on the server; skipping.", name);
                continue;
            }
            if (!warmed.Add(language.Key)) continue; // already warmed (e.g. the default) — don't double-fetch
            await WarmCacheAsync(language.Key, application);
        }
    }

    private async Task<ContentResult> FetchContentWithTimeout(string key, string cacheKey, string defaultValue, Guid languageKey, ContentFormat? contentType, Stopwatch sw, string effectiveApplication, IReadOnlyDictionary<string, string> translations = null)
    {
        try
        {
            var request = new GetContentRequest
            {
                Key = key,
                LanguageKey = languageKey,
                Application = effectiveApplication,
                Environment = _environmentName.Name,
                Instance = null,
                DefaultValue = contentType == null ? null : $"{defaultValue}",
                ContentFormat = contentType,
                Translations = translations
            };
            var complexKey = BuildKey(request);

            using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
            using var client = GetHttpClient();
            var address = $"Api/Content/{complexKey}";
            var response = await client.GetAsync(address, cts.Token);

            WarnIfSlow(sw.ElapsedMilliseconds, $"content load for '{key}' (language {languageKey})", (int)response.StatusCode, response.ReasonPhrase);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Information, not Debug: an unseeded key is actionable — someone should seed it —
                    // and Debug is not enabled in practice, so it was effectively invisible. Safe to
                    // raise because CacheFailure() below negative-caches the key, so this fires once
                    // per key per FailureCacheDuration rather than once per render. Still not a
                    // Warning: falling back to the caller's Default is the designed behaviour.
                    _logger.LogInformation("No content override for key '{Key}' (404). Using default value.", key);
                }
                else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Warning, not Error: a 429 is the server applying backpressure, which is designed
                    // behaviour on both sides — not a fault to page someone about. Logging it at Error
                    // would make a healthy shed look like an outage, and under load it is the single
                    // most repeated line there is.
                    _logger.LogWarning("Rate limited (429) getting content for key '{Key}'. Backing off for {RetryAfter}.",
                        key, RetryAfterOf(response)?.ToString() ?? "the default failure duration");
                }
                else
                {
                    _logger.LogError("Unable to get content for key '{Key}'. Response was {StatusCode} {ReasonPhrase}.",
                        key, response.StatusCode, response.ReasonPhrase);
                }

                // Prefer a value already cached over the caller's default. A 429 says nothing about the
                // content, so overwriting a good Swedish value with the English default would make
                // backpressure look like a translation regression — the half-translated page in
                // Toolkit issue #172. The other failure branches keep the same preference.
                var fallbackValue = _localCache.TryGetValue(cacheKey, out var cachedOnFailure)
                    ? cachedOnFailure.Value
                    : defaultValue;

                // Negative-cache either way so the key isn't re-requested (and re-logged) every render.
                CacheFailure(cacheKey, fallbackValue, RetryAfterOf(response));
                LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, ContentSource.Default, stale: true);
                return Result(fallbackValue, false, ContentSource.Default, true);
            }

            var result = await response.Content.ReadFromJsonAsync<GetContentResponse>(cancellationToken: cts.Token);

            var interval = result.ValidTo - DateTime.UtcNow;
            if (interval > TimeSpan.Zero)
                _lastKnownTtl[cacheKey] = interval;

            var cached = new CachedContent
            {
                Value = result.Value,
                ValidTo = result.ValidTo,
                ServedLanguageKey = result.ServedLanguageKey,
                FallbackReason = result.FallbackReason,
                IsStageFallback = result.IsStageFallback,
            };
            _localCache.AddOrUpdate(cacheKey, cached, (_, _) => cached);

            LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, ContentSource.Server, stale: false);
            return Result(result.Value ?? defaultValue, true, ContentSource.Server, false, cached);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTTP request timed out for content '{Key}' after {Timeout}ms. Using default value.",
                key, _contentOptions.HttpTimeout.TotalMilliseconds);
            CacheFailure(cacheKey, defaultValue);
            LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, ContentSource.Default, stale: true);
            return Result(defaultValue, false, ContentSource.Default, true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Using default for content key {Key}.", e.Message, key);
            CacheFailure(cacheKey, defaultValue);
            LogResolved(key, languageKey, effectiveApplication, sw.ElapsedMilliseconds, ContentSource.Default, stale: true);
            return Result(defaultValue, false, ContentSource.Default, true);
        }
    }

    private void StartBackgroundRefresh(string key, string cacheKey, string defaultValue, Guid languageKey, ContentFormat? contentType, string effectiveApplication, IReadOnlyDictionary<string, string> translations = null)
    {
        if (!_refreshInProgress.TryAdd(cacheKey, true)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var request = new GetContentRequest
                {
                    Key = key,
                    LanguageKey = languageKey,
                    Application = effectiveApplication,
                    Environment = _environmentName.Name,
                    Instance = null,
                    DefaultValue = contentType == null ? null : $"{defaultValue}",
                    ContentFormat = contentType,
                    Translations = translations
                };
                var complexKey = BuildKey(request);

                using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
                using var client = GetHttpClient();
                var address = $"Api/Content/{complexKey}";
                var response = await client.GetAsync(address, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Same reasoning as the foreground 404 above: actionable, and deduped by the
                        // negative cache to once per key per refresh cycle.
                        _logger.LogInformation("Background refresh for content '{Key}': no override (404). Keeping default value.", key);
                    }
                    else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Background refresh for content '{Key}' was rate limited (429). Backing off for {RetryAfter}.",
                            key, RetryAfterOf(response)?.ToString() ?? "the default failure duration");
                    }
                    else
                    {
                        _logger.LogError("Background refresh for content '{Key}' failed. Response was {StatusCode} {ReasonPhrase}.",
                            key, response.StatusCode, response.ReasonPhrase);
                    }
                    var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                    CacheFailure(cacheKey, staleValue, RetryAfterOf(response));
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<GetContentResponse>(cancellationToken: cts.Token);

                var interval = result.ValidTo - DateTime.UtcNow;
                if (interval > TimeSpan.Zero)
                    _lastKnownTtl[cacheKey] = interval;

                var refreshed = new CachedContent
                {
                    Value = result.Value,
                    ValidTo = result.ValidTo,
                    ServedLanguageKey = result.ServedLanguageKey,
                    FallbackReason = result.FallbackReason,
                    IsStageFallback = result.IsStageFallback,
                };
                _localCache.AddOrUpdate(cacheKey, refreshed, (_, _) => refreshed);
                _logger.LogInformation("Background refresh for content '{Key}' completed. ValidTo: {ValidTo}.",
                    key, result.ValidTo);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Background refresh for content '{Key}' timed out after {Timeout}ms.",
                    key, _contentOptions.HttpTimeout.TotalMilliseconds);
                var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                CacheFailure(cacheKey, staleValue);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Background refresh for content '{Key}' failed: {Message}.", key, e.Message);
                var staleValue = _localCache.TryGetValue(cacheKey, out var s) ? s.Value : defaultValue;
                CacheFailure(cacheKey, staleValue);
            }
            finally
            {
                _refreshInProgress.TryRemove(cacheKey, out _);
            }
        });
    }

    // #132 diagnostics: one consistent Debug line per content resolution, carrying the resolved
    // language + application so a slow or looping content load can be traced by key+language.
    // These fire on every read (typically a 0ms cache hit) so they stay at Debug — opt-in via the
    // category Quilt4Net.Toolkit.Features.Content.RemoteContentCallService at Debug.
    private void LogResolved(string key, Guid languageKey, string application, long elapsedMs, ContentSource source, bool stale)
    {
        _logger.LogDebug(
            "Content '{Key}' (language {LanguageKey}, application '{Application}') resolved in {Elapsed}ms. Source: {Source}, Stale: {Stale}.",
            key, languageKey, application ?? "", elapsedMs, source, stale);
    }

    // #132 diagnostics: surface a genuinely slow server round-trip as a single Warning (visible
    // without Debug logging) so consumers can tell network latency apart from cache/render cost.
    // Gated by ContentOptions.SlowLogThreshold (TimeSpan.Zero disables). Only called after a
    // completed HTTP response, so a timeout — which already logs its own Warning — isn't double-counted.
    private void WarnIfSlow(long elapsedMs, string what, int statusCode, string reasonPhrase)
    {
        var threshold = _contentOptions.SlowLogThreshold;
        if (threshold <= TimeSpan.Zero || elapsedMs < threshold.TotalMilliseconds) return;
        _logger.LogWarning("Slow {What}: {Elapsed}ms → {StatusCode} {ReasonPhrase} (threshold {Threshold}ms).",
            what, elapsedMs, statusCode, reasonPhrase, (long)threshold.TotalMilliseconds);
    }

    // Negative cache. IsDefault marks the entry as a fallback rather than server content, so a later
    // hit reports ContentSource.Default instead of masquerading as a cache hit.
    /// <summary>
    /// How long to hold off after a <c>429 Too Many Requests</c>, from the server's own
    /// <c>Retry-After</c> header. Returns null when the response is not a 429 or carries no usable
    /// header, in which case the caller's normal negative-cache duration applies.
    /// </summary>
    /// <remarks>
    /// The server is telling us when it will be ready. Ignoring that and falling back to the last
    /// successful TTL — which is a *content freshness* interval and has nothing to do with
    /// backpressure — either retries far too early and deepens the overload that caused the 429, or
    /// sits out a multi-minute TTL when the server asked for a few seconds.
    /// <para>
    /// RFC 9110 allows either delta-seconds or an HTTP-date; <see cref="HttpResponseHeaders.RetryAfter"/>
    /// surfaces both, so both are handled. A date in the past yields <see cref="TimeSpan.Zero"/>, which
    /// is treated as "no advice" rather than "retry immediately".
    /// </para>
    /// </remarks>
    private static TimeSpan? RetryAfterOf(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests) return null;

        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter == null) return null;

        if (retryAfter.Delta is { } delta && delta > TimeSpan.Zero) return delta;

        if (retryAfter.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) return wait;
        }

        return null;
    }

    private void CacheFailure(string cacheKey, string value, TimeSpan? overrideDuration = null)
    {
        var duration = overrideDuration
                       ?? _lastKnownTtl.GetValueOrDefault(cacheKey, _contentOptions.FailureCacheDuration);
        var failureResponse = new CachedContent
        {
            Value = value,
            ValidTo = DateTime.UtcNow.Add(duration),
            IsDefault = true
        };
        _localCache.AddOrUpdate(cacheKey, failureResponse, (_, _) => failureResponse);
    }

    private string ResolveApplication(string application)
    {
        // The convention: null is the "default" sentinel — toolkit looks up the application.
        // "" is forwarded as-is (= shared). A non-empty value is forwarded as-is.
        if (application != null) return application;
        return _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;
    }

    private static string BuildCacheKey(string key, Guid languageKey, string effectiveApplication)
    {
        // "" and null both mean "shared" so they collapse to the same cache slot.
        return $"{key}_{languageKey}|{effectiveApplication ?? ""}";
    }

    public IReadOnlyDictionary<Guid, int> GetCacheCountsByLanguage()
    {
        var counts = new Dictionary<Guid, int>();
        foreach (var cacheKey in _localCache.Keys)
        {
            if (!TryParseLanguageFromCacheKey(cacheKey, out var languageKey)) continue;
            counts[languageKey] = counts.GetValueOrDefault(languageKey) + 1;
        }
        return counts;
    }

    // Reverse of BuildCacheKey: the language key is the 36-char GUID immediately before the '|'
    // application delimiter (application names never contain '|'). Kept beside BuildCacheKey so the
    // two stay in sync if the format ever changes.
    private static bool TryParseLanguageFromCacheKey(string cacheKey, out Guid languageKey)
    {
        languageKey = Guid.Empty;
        var pipe = cacheKey.LastIndexOf('|');
        if (pipe < 36) return false;
        return Guid.TryParse(cacheKey.Substring(pipe - 36, 36), out languageKey);
    }

    private static string BuildKey(GetContentRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        var payload = WebUtility.UrlEncode(base64);
        return payload;
    }

    // Factory-created named client: BaseAddress + X-API-KEY are configured once at registration,
    // and the correlation-id handler is attached there. Disposing a factory client is the intended
    // usage (it returns the pooled handler), so existing `using var client = GetHttpClient();`
    // call sites stay correct.
    private HttpClient GetHttpClient() => _httpClientFactory.CreateClient(HttpClientName);
}