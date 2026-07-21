using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Pre-fills the content cache at application startup via one bulk call per language, so the first
/// page render serves from a warm cache instead of fanning out a request per key. Warms the default
/// language plus any listed in <see cref="ContentOptions.WarmUpLanguages"/>. Runs in the background
/// (does not block startup) and is best-effort — any failure is swallowed, leaving the normal
/// per-key path intact. Disabled via <see cref="ContentOptions.WarmUpEnabled"/>. Languages selected
/// at runtime but not pre-warmed are warmed per-circuit by <see cref="LanguageStateService"/>.
/// </summary>
internal sealed class ContentWarmupHostedService : IHostedService
{
    private readonly IRemoteContentCallService _callService;
    private readonly ContentOptions _options;
    private readonly ILogger<ContentWarmupHostedService> _logger;

    public ContentWarmupHostedService(IRemoteContentCallService callService, IOptions<ContentOptions> options, ILogger<ContentWarmupHostedService> logger)
    {
        _callService = callService;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.WarmUpEnabled) return Task.CompletedTask;

        // Background so app startup isn't blocked by the bulk fetch. Warms the default language plus
        // any configured in ContentOptions.WarmUpLanguages.
        _ = Task.Run(async () =>
        {
            try
            {
                await _callService.WarmConfiguredLanguagesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Content startup warm-up failed: {Message}", e.Message);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
