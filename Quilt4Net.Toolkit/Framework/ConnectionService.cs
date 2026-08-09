using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Framework;

internal class ConnectionService : IConnectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// How long an unsuccessful probe is trusted before it is worth asking again.
    /// <para>
    /// A failure used to be either cached for the lifetime of one short-lived service instance or
    /// (in the exception case) not cached at all. Now that the cache is shared process-wide, caching
    /// "forever" would turn one blip during startup into a permanently disconnected UI, while caching
    /// nothing means every probe after a failure re-pays the full round trip (#156). A minute kills
    /// the burst and still recovers on its own.
    /// </para>
    /// </summary>
    internal TimeSpan FailureCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    private readonly ContentOptions _contentOptions;
    private readonly RemoteConfigurationOptions _configurationOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Probe results, shared by every caller in the process. This service used to be transient, so
    /// the cache lived and died with a single component's injected copy: the "cached" probe was in
    /// practice re-issued per component, per circuit (#156).
    /// </summary>
    private readonly ConcurrentDictionary<Service, CacheEntry> _cache = new();

    public ConnectionService(IOptions<ContentOptions> contentOptions, IOptions<RemoteConfigurationOptions> configurationOptions, IHttpClientFactory httpClientFactory)
    {
        _contentOptions = contentOptions.Value;
        _configurationOptions = configurationOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ConnectionResult> CanConnectAsync(Service service)
    {
        if (_cache.TryGetValue(service, out var cached) && !cached.HasExpired(DateTime.UtcNow))
            return cached.Result;

        if (!TryGetConfiguration(service, out var config, out var configError))
        {
            // Cache the unconfigured result so subsequent probes return immediately
            // instead of paying the upstream HealthService timeout per call. No expiry:
            // missing configuration cannot fix itself while the process runs.
            var unconfigured = new ConnectionResult { Success = false, Message = configError };
            _cache[service] = CacheEntry.Permanent(unconfigured);
            return unconfigured;
        }

        try
        {
            var client = GetHttpClient(service, config);

            var response = await client.GetAsync("Api/System/WhoAmI");

            WhoAmIResponse capabilities = null;
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                capabilities = JsonSerializer.Deserialize<WhoAmIResponse>(json, JsonOptions);
            }

            var result = new ConnectionResult
            {
                Success = response.IsSuccessStatusCode,
                Message = response.ReasonPhrase,
                Address = config.BaseAddress,
                Capabilities = capabilities
            };

            _cache[service] = result.Success
                ? CacheEntry.Permanent(result)
                : CacheEntry.Expiring(result, DateTime.UtcNow.Add(FailureCacheDuration));
            return result;
        }
        catch (Exception e)
        {
            var result = new ConnectionResult
            {
                Success = false,
                Message = e.Message,
                Address = config.BaseAddress
            };

            // Cached like any other failure. Leaving this path uncached was the one asymmetry in
            // the original three: an unreachable server made every later probe pay the full timeout.
            _cache[service] = CacheEntry.Expiring(result, DateTime.UtcNow.Add(FailureCacheDuration));
            return result;
        }
    }

    /// <summary>
    /// The pooled client for the probed service. The named clients registered by
    /// <c>AddQuilt4NetContent</c> / <c>AddQuilt4NetRemoteConfiguration</c> already carry the same
    /// base address, API key and correlation-id handler, so the probe reuses one rather than
    /// constructing (and discarding) an <see cref="HttpClient"/> of its own per call.
    /// </summary>
    /// <remarks>
    /// A host that registered only one of the two features still has the *other* service's named
    /// client resolvable — the factory hands back an unconfigured client for any name. That is what
    /// the <c>BaseAddress</c> check covers. Nothing is added on top of an already-configured client:
    /// re-adding X-API-KEY would send it twice, which the server rejects as an invalid key.
    /// </remarks>
    private HttpClient GetHttpClient(Service service, (Uri BaseAddress, string ApiKey) config)
    {
        var name = service switch
        {
            Service.Content => Features.Content.RemoteContentCallService.HttpClientName,
            Service.Configuration => Features.FeatureToggle.RemoteConfigCallService.HttpClientName,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };

        var client = _httpClientFactory.CreateClient(name);
        if (client.BaseAddress != null) return client;

        client.BaseAddress = config.BaseAddress;
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            client.DefaultRequestHeaders.Remove("X-API-KEY");
            client.DefaultRequestHeaders.Add("X-API-KEY", config.ApiKey);
        }
        return client;
    }

    /// <summary>
    /// Resolves the per-service base address + api key. Returns false (not throws) when
    /// the configured address is missing or unparseable, so the caller can surface an
    /// <c>Unhealthy</c> result with an actionable message instead of letting
    /// <c>new Uri(null)</c> escape past <see cref="CanConnectAsync"/>'s try/catch.
    /// </summary>
    private bool TryGetConfiguration(Service service, out (Uri BaseAddress, string ApiKey) config, out string error)
    {
        var (address, apiKey, optionsName, registrationName) = service switch
        {
            Service.Content => (_contentOptions?.Quilt4NetAddress, _contentOptions?.ApiKey, nameof(ContentOptions), "AddQuilt4NetContent"),
            Service.Configuration => (_configurationOptions?.Quilt4NetAddress, _configurationOptions?.ApiKey, nameof(RemoteConfigurationOptions), "AddQuilt4NetRemoteConfiguration"),
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };

        if (string.IsNullOrWhiteSpace(address))
        {
            config = default;
            error = $"{optionsName}.Quilt4NetAddress is not configured. Call {registrationName}() during startup or set Quilt4Net:{optionsName.Replace("Options", string.Empty)}:Quilt4NetAddress (or Quilt4Net:Quilt4NetAddress) in configuration.";
            return false;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            config = default;
            error = $"{optionsName}.Quilt4NetAddress '{address}' is not a valid absolute URI.";
            return false;
        }

        config = (uri, apiKey);
        error = null;
        return true;
    }

    private readonly record struct CacheEntry(ConnectionResult Result, DateTime? ExpiresUtc)
    {
        public static CacheEntry Permanent(ConnectionResult result) => new(result, null);
        public static CacheEntry Expiring(ConnectionResult result, DateTime expiresUtc) => new(result, expiresUtc);
        public bool HasExpired(DateTime nowUtc) => ExpiresUtc.HasValue && ExpiresUtc.Value <= nowUtc;
    }
}
