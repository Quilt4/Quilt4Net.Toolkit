using Quilt4Net.Toolkit.Features.Probe;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

internal class MyHostedService : IHostedService
{
    private readonly IHostedServiceProbe<MyHostedService> _hostedServiceProbe;

    public MyHostedService(IHostedServiceProbe<MyHostedService> hostedServiceProbe)
    {
        _hostedServiceProbe = hostedServiceProbe;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hostedServiceProbe.EndService(true);
        return Task.CompletedTask;
    }
}