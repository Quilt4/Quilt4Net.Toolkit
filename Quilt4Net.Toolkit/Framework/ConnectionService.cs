using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Framework;

internal class ConnectionService : IConnectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ContentOptions _contentOptions;
    private readonly RemoteConfigurationOptions _configurationOptions;
    private readonly Dictionary<Service, ConnectionResult> _cache = new();

    public ConnectionService(IOptions<ContentOptions> contentOptions, IOptions<RemoteConfigurationOptions> configurationOptions)
    {
        _contentOptions = contentOptions.Value;
        _configurationOptions = configurationOptions.Value;
    }

    public async Task<ConnectionResult> CanConnectAsync(Service service)
    {
        if (_cache.TryGetValue(service, out var cached))
            return cached;

        if (!TryGetConfiguration(service, out var config, out var configError))
        {
            // Cache the unconfigured result so subsequent probes return immediately
            // instead of paying the upstream HealthService timeout per call.
            var unconfigured = new ConnectionResult { Success = false, Message = configError };
            _cache[service] = unconfigured;
            return unconfigured;
        }

        try
        {
            using var client = new HttpClient();

            client.BaseAddress = config.BaseAddress;
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", config.ApiKey);
            }

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

            _cache[service] = result;
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
            return result;
        }
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
}