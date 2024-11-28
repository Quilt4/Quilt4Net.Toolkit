using Microsoft.Extensions.Hosting;
using System.Reflection;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Version;

internal class VersionService : IVersionService
{
    private readonly IHostEnvironment _hostEnvironment;

    public VersionService(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken)
    {
        var asm = Assembly.GetEntryAssembly();
        var name = _hostEnvironment.EnvironmentName;
        var ipAddress = await GetExternalIpAsync(true); //TODO: This should be optional

        var result = new VersionResponse
        {
            Version = $"{asm?.GetName().Version}",
            Machine = Environment.MachineName,
            Environment = name,
            IpAddress = ipAddress
        };

        return result;
    }

    private async Task<string> GetExternalIpAsync(bool showIp)
    {
        if (!showIp) return null;

        try
        {
            using var client = new HttpClient();
            var result = await client.GetStringAsync(new Uri("http://ipv4.icanhazip.com/"));
            return result.TrimEnd('\n');
        }
        catch (Exception e)
        {
            return $"Unknown ({e.Message})";
        }
    }
}