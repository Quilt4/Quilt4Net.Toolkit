using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Probe;

internal class HostedServiceProbeRegistry : IHostedServiceProbeRegistry
{
    private readonly List<IHostedServiceProbe> _probes = new();

    public void Register(IHostedServiceProbe hostedServiceProbe)
    {
        _probes.Add(hostedServiceProbe);
    }

    public async IAsyncEnumerable<KeyValuePair<string, HealthComponent>> GetProbesAsync()
    {
        foreach (var probe in _probes)
        {
            yield return new KeyValuePair<string, HealthComponent>(probe.Name, probe.GetHealth());
        }
    }
}