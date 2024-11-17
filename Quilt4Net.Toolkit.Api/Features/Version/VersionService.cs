using Microsoft.Extensions.Hosting;
using System.Reflection;

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

        //TODO: Look up IP address here.

        var result = new VersionResponse
        {
            Version = $"{asm?.GetName().Version}",
            Machine = Environment.MachineName,
            Environment = name
        };

        return result;
    }
}