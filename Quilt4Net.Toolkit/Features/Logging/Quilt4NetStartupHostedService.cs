using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Logging;

/// <summary>
/// Emits a single Information-level log entry on application startup with the
/// resolved Quilt4NetLoggingOptions. The entry carries a Quilt4NetStartup=true
/// structured property so observability tooling can locate startup events
/// quickly (e.g. for the application/environment version matrix view).
/// </summary>
internal sealed class Quilt4NetStartupHostedService : IHostedService
{
    private readonly ILogger<Quilt4NetStartupHostedService> _logger;
    private readonly Quilt4NetLoggingOptions _options;

    public Quilt4NetStartupHostedService(ILogger<Quilt4NetStartupHostedService> logger, Quilt4NetLoggingOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Quilt4NetStartupLogger.Log(_logger, _options);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
