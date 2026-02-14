using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Health.Features.Heartbeat;

internal class HeartbeatBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HeartbeatOptions _heartbeatOptions;
    private readonly ILogger<HeartbeatBackgroundService> _logger;
    private readonly TaskCompletionSource _startSignal = new();

    public HeartbeatBackgroundService(IServiceProvider serviceProvider, HeartbeatOptions heartbeatOptions, ILogger<HeartbeatBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _heartbeatOptions = heartbeatOptions;
        _logger = logger;
    }

    internal void Start()
    {
        _startSignal.TrySetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _startSignal.Task.WaitAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var heartbeatService = scope.ServiceProvider.GetRequiredService<IHeartbeatService>();
                await heartbeatService.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Heartbeat execution failed.");
            }

            await Task.Delay(_heartbeatOptions.Interval, stoppingToken);
        }
    }
}
