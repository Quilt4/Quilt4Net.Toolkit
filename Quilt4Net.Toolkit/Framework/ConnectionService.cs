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

        var config = GetConfiguration(service);

        try
        {
            using var client = new HttpClient();

            client.BaseAddress = config.BaseAddress;
            client.DefaultRequestHeaders.Add("X-API-KEY", config.ApiKey);

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

    private (Uri BaseAddress, string ApiKey) GetConfiguration(Service service)
    {
        return service switch
        {
            Service.Content => (new Uri(_contentOptions.Quilt4NetAddress), _contentOptions.ApiKey),
            Service.Configuration => (new Uri(_configurationOptions.Quilt4NetAddress), _configurationOptions.ApiKey),
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }
}