using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Pre-fills the content cache with the default language at application startup via one bulk call,
/// so the first page render serves from a warm cache instead of fanning out a request per key.
/// Runs in the background (does not block startup) and is best-effort — any failure is swallowed by
/// the warm-up call, leaving the normal per-key path intact. Disabled via <see cref="ContentOptions.WarmUpEnabled"/>.
/// The selected (non-default) language is warmed per-circuit by <see cref="LanguageStateService"/>.
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

        // Background so app startup isn't blocked by the bulk fetch. Guid.Empty = default language.
        _ = Task.Run(async () =>
        {
            try
            {
                await _callService.WarmCacheAsync(Guid.Empty);
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
