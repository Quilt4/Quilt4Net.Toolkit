using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Blazor;

/// <summary>
/// Keeps the content cache filled from the bulk endpoint: once at application startup, and then
/// repeatedly at <see cref="ContentOptions.WarmUpRefreshFraction"/> of the server's own content
/// lifetime, so entries are replaced before they expire. Warms the default language, any listed in
/// <see cref="ContentOptions.WarmUpLanguages"/>, and any language selected at runtime. Runs in the
/// background (does not block startup) and is best-effort throughout — any failure leaves the normal
/// per-key path intact. Disabled via <see cref="ContentOptions.WarmUpEnabled"/>; the repeat alone via
/// <see cref="ContentOptions.PeriodicWarmUpEnabled"/>.
/// </summary>
/// <remarks>
/// The repeat is the point (issue #163). A bulk warm writes one shared <c>ValidTo</c> across every
/// key, so a single warm-up means the entire set expires at the same instant and the next render
/// fans out one call per key — 721 of them in the reported case. That burst is what exceeds the
/// server's per-caller limit, and the queued requests then outlive the client's timeout, which
/// negative-caches, which expires into the next burst. Re-warming before expiry removes the burst
/// rather than spreading it.
/// </remarks>
internal sealed class ContentWarmupHostedService : BackgroundService
{
    /// <summary>Used until the server's real content lifetime has been observed once.</summary>
    private static readonly TimeSpan UnknownTtlInterval = TimeSpan.FromMinutes(10);

    private readonly IRemoteContentCallService _callService;
    private readonly IConnectionService _connectionService;
    private readonly ContentOptions _options;
    private readonly ILogger<ContentWarmupHostedService> _logger;

    public ContentWarmupHostedService(IRemoteContentCallService callService, IConnectionService connectionService, IOptions<ContentOptions> options, ILogger<ContentWarmupHostedService> logger)
    {
        _callService = callService;
        _connectionService = connectionService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WarmUpEnabled) return;

        // Hand control back to the host immediately: BackgroundService runs this from StartAsync, and
        // startup must not wait on a bulk fetch.
        await Task.Yield();

        await WarmAsync();

        // The connectivity probe is warmed here too (#156). Its result is now shared process-wide, so
        // paying for it once at startup keeps it off the render path of the first circuit — which is
        // the one where a user actually notices it.
        try
        {
            await _connectionService.CanConnectAsync(Service.Content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Connection probe warm-up failed: {Message}", e.Message);
        }

        if (!_options.PeriodicWarmUpEnabled) return;

        using var timer = new PeriodicTimer(NextInterval());
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await WarmAsync();

            // Re-read every cycle: the server's lifetime is configurable per team and the first
            // pass may have run before any of it was known.
            timer.Period = NextInterval();
        }
    }

    private async Task WarmAsync()
    {
        try
        {
            await _callService.WarmConfiguredLanguagesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Content warm-up failed: {Message}", e.Message);
        }
    }

    /// <summary>
    /// How long until the next re-warm: a fraction of the lifetime the server itself reported, so
    /// the client follows the server's TTL rather than a number of its own.
    /// </summary>
    internal TimeSpan NextInterval()
    {
        var lifetime = _callService.ObservedContentTtl ?? UnknownTtlInterval;
        var fraction = Math.Clamp(_options.WarmUpRefreshFraction, 0.1, 0.95);
        var interval = lifetime * fraction;
        var floor = _options.MinimumWarmUpInterval > TimeSpan.Zero ? _options.MinimumWarmUpInterval : TimeSpan.Zero;
        return interval < floor ? floor : interval;
    }
}
