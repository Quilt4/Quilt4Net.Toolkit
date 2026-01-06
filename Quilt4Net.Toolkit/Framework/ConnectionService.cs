using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Framework;

internal class ConnectionService : IConnectionService
{
    private readonly ContentOptions _contentOptions;
    private readonly RemoteConfigurationOptions _configurationOptions;

    public ConnectionService(IOptions<ContentOptions> contentOptions, IOptions<RemoteConfigurationOptions> configurationOptions)
    {
        _contentOptions = contentOptions.Value;
        _configurationOptions = configurationOptions.Value;
    }

    public async Task<(bool Success, string Message, Uri Address)> CanConnectAsync(Service service)
    {
        var config = GetConfiguration(service);

        try
        {
            using var client = new HttpClient();

            client.BaseAddress = config.BaseAddress;
            client.DefaultRequestHeaders.Add("X-API-KEY", config.ApiKey);

            var response = await client.GetAsync("Api/System/WhoAmI");
            return (response.IsSuccessStatusCode, response.ReasonPhrase, config.BaseAddress);
        }
        catch (Exception e)
        {
            return (false, e.Message, config.BaseAddress);
        }
    }

    private (Uri BaseAddress, string ApiKey) GetConfiguration(Service service)
    {
        (Uri BaseAddress, string ApiKey) x;
        switch (service)
        {
            case Service.Content:
                x.BaseAddress = new Uri(_contentOptions.Quilt4NetAddress);
                x.ApiKey = _contentOptions.ApiKey;
                break;
            case Service.Configuration:
                x.BaseAddress = new Uri(_configurationOptions.Quilt4NetAddress);
                x.ApiKey = _configurationOptions.ApiKey;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(service), service, null);
        }

        return x;
    }
}