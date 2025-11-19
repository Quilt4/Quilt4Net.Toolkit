using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Features.Health;
using System.Reflection;

namespace Quilt4Net.Toolkit.Api.Features.Version;

/// <summary>
/// Service for Version.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Builds the version and environment information.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}

internal class VersionService : IVersionService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Quilt4NetApiOptions _options;

    public VersionService(IHostEnvironment hostEnvironment, Quilt4NetApiOptions options)
    {
        _hostEnvironment = hostEnvironment;
        _options = options;
    }

    public async Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken)
    {
        var asm = Assembly.GetEntryAssembly();
        var name = _hostEnvironment.EnvironmentName;
        var ipAddress = await GetExternalIpAsync(_options.IpAddressCheckUri);

        var result = new VersionResponse
        {
            Version = $"{asm?.GetName().Version}",
            Machine = Environment.MachineName,
            Environment = name,
            IpAddress = ipAddress,
            Is64BitProcess = Environment.Is64BitProcess
        };

        return result;
    }

    private async Task<string> GetExternalIpAsync(Uri ipAddressCheck)
    {
        if (ipAddressCheck == null) return null;

        try
        {
            using var client = new HttpClient();
            var result = await client.GetStringAsync(ipAddressCheck);
            return result.TrimEnd('\n');
        }
        catch (Exception e)
        {
            return $"Unknown ({e.Message})";
        }
    }
}