using Quilt4Net.Toolkit.Features.Probe;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

internal class MyBackgroundService : BackgroundService
{
    private readonly IHostedServiceProbe _hostedServiceProbe;
    private readonly Random _rng;

    public MyBackgroundService(IHostedServiceProbe<MyBackgroundService> hostedServiceProbe)
    {
        _hostedServiceProbe = hostedServiceProbe.Register();
        _rng = new Random();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            var n = 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                //var n = _rng.Next(0, 5);
                //var time = TimeSpan.FromSeconds(10 + n);
                //var time = TimeSpan.FromMinutes(1);
                var time = TimeSpan.FromMilliseconds(Math.Pow(2, n++));
                await Task.Delay(time, stoppingToken);
                _hostedServiceProbe.Pulse();
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }
}