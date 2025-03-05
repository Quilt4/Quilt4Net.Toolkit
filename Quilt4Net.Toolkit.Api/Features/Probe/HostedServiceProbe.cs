using System.Diagnostics;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Probe;

internal class HostedServiceProbe<TComponent> : HostedServiceProbe, IHostedServiceProbe<TComponent>
{
    public HostedServiceProbe(IHostedServiceProbeRegistry hostedServiceProbeRegistry)
        : base(hostedServiceProbeRegistry)
    {
    }

    public IHostedServiceProbe Register(TimeSpan? plannedInterval = default, bool autoMaxInterval = true)
    {
        return Register(Name, plannedInterval, autoMaxInterval);
    }

    public override string Name => typeof(TComponent).Name;
}

internal class HostedServiceProbe : IHostedServiceProbe
{
    private readonly List<long> _pulseTimes = new();
    private readonly Stopwatch _stopwatch = new();
    private bool _isFirstPulse = true;
    private string _name = "Unknown";
    private Exception _exception;
    private bool _ended;
    private TimeSpan? _plannedInterval;
    private bool _autoMaxInterval;

    public HostedServiceProbe(IHostedServiceProbeRegistry hostedServiceProbeRegistry)
    {
        hostedServiceProbeRegistry.Register(this);
        _stopwatch.Start();
    }

    public virtual string Name => _name;

    public void Pulse()
    {
        // Starta mätningen vid första anropet
        if (_isFirstPulse)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            _isFirstPulse = false;
        }

        // Logga tidpunkten för detta anrop
        _pulseTimes.Add(_stopwatch.ElapsedMilliseconds);

        //NOTE: Reset end/exception if restarted.
        _ended = false;
        _exception = null;
    }

    public IHostedServiceProbe Register(string name, TimeSpan? plannedInterval = default, bool autoMaxInterval = true)
    {
        _name = name;
        _plannedInterval = plannedInterval;
        _autoMaxInterval = autoMaxInterval;
        return this;
    }

    public void EndService(bool success)
    {
        if (!success) _exception = new Exception("Unknown");
        _ended = true;
    }

    public void EndService(Exception exception)
    {
        _exception = exception;
        _ended = true;
    }

    public HealthComponent GetHealth()
    {
        if (_ended)
        {
            var message = _exception == null ? "Ended successfully." : $"Ended with exception. {_exception.Message}";
            return new HealthComponent
            {
                Status = _exception == null ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Details = new Dictionary<string, string>
                {
                    { "message", message }
                }
            };
        }

        if (_pulseTimes.Count < 2)
        {
            return BuildPreHealthComponent();
        }

        //// Beräkna tidsintervall mellan pulser
        //List<long> intervals = new();
        //for (var i = 1; i < _pulseTimes.Count; i++)
        //{
        //    intervals.Add(_pulseTimes[i] - _pulseTimes[i - 1]);
        //}

        //// Genomsnittligt intervall
        //var averageInterval = intervals.Average();
        //var averageFrequency = 1000 / averageInterval;
        //var averagePulseInterval = TimeSpan.FromMilliseconds(averageInterval);

        //// Stabilitetsfaktor (standardavvikelse)
        //var variance = intervals.Select(interval => Math.Pow(interval - averageInterval, 2)).Average();
        //var standardDeviation = Math.Sqrt(variance);

        //// Analysera senaste data
        //var lastInterval = intervals.Last();

        //// Tid sedan senaste puls
        //var elapsedSinceLastPulse = _stopwatch.ElapsedMilliseconds - _pulseTimes.Last();
        //var lastPulse = TimeSpan.FromMilliseconds(elapsedSinceLastPulse);

        //// Nästa förväntade puls
        //var nextExpectedPuse = TimeSpan.FromMilliseconds(averageInterval - elapsedSinceLastPulse);

        //// Bestäm status baserat på standardavvikelse och senaste intervallet
        //HealthStatus state;
        //string reason;

        //if (lastInterval <= averageInterval + 2 * standardDeviation)
        //{
        //    state = HealthStatus.Healthy;
        //    reason = "Pulse is occurring at expected intervals.";
        //}
        //else if (lastInterval <= averageInterval + 4 * standardDeviation)
        //{
        //    state = HealthStatus.Degraded;
        //    reason = "Pulse frequency has slowed or become irregular.";
        //}
        //else
        //{
        //    state = HealthStatus.Unhealthy;
        //    reason = "Pulse appears to have stopped.";
        //}

        //// Returnera status
        //return new HealthComponent
        //{
        //    Status = state,
        //    Details = new Dictionary<string, string>
        //    {
        //        { "message", reason },
        //        { "averageFrequency", $"{averageFrequency}" },
        //        { "averageInterval", $"{averagePulseInterval}" },
        //        { "standardDeviation", $"{standardDeviation}" },
        //        { "lastPulse", $"{lastPulse}" },
        //        { "nextExpectedPuse", $"{nextExpectedPuse}" },
        //        { "pulseCount", $"{_pulseTimes.Count}" },
        //    }
        //};

        // Calculate intervals between pulses
        List<long> intervals = new();
        for (int i = 1; i < _pulseTimes.Count; i++)
        {
            intervals.Add(_pulseTimes[i] - _pulseTimes[i - 1]);
        }

        double averageInterval = intervals.Average();
        double variance = intervals.Select(interval => Math.Pow(interval - averageInterval, 2)).Average();
        double standardDeviation = Math.Sqrt(variance);

        // Time since last pulse
        long elapsedSinceLastPulse = _stopwatch.ElapsedMilliseconds - _pulseTimes.Last();
        TimeSpan timeSinceLastPulse = TimeSpan.FromMilliseconds(elapsedSinceLastPulse);

        //Extra
        var averageFrequency = 1000 / averageInterval;
        var averagePulseInterval = TimeSpan.FromMilliseconds(averageInterval);
        var maxPulseInterval = TimeSpan.FromMilliseconds(intervals.Any() ? intervals.Max() : 0);
        var lastPulse = TimeSpan.FromMilliseconds(elapsedSinceLastPulse);
        var nextExpectedPuse = TimeSpan.FromMilliseconds(averageInterval - elapsedSinceLastPulse);

        // Determine state
        HealthStatus state;
        string reason;
        DateTime nextExpectedPulse = DateTime.UtcNow.AddMilliseconds(averageInterval);

        // Logic for determining status
        if (_plannedInterval.HasValue && elapsedSinceLastPulse < _plannedInterval.Value.TotalMilliseconds)
        {
            //Never report issue if the planned interval has not been reached.
            state = HealthStatus.Healthy;
            reason = "Pulse have not reached planned interval.";
        }
        else if (_autoMaxInterval && elapsedSinceLastPulse < maxPulseInterval.TotalMilliseconds)
        {
            //Never report issue if the maximum interval has not been reached.
            state = HealthStatus.Healthy;
            reason = "Pulse have not reached maximum interval.";
        }
        else if (elapsedSinceLastPulse <= averageInterval + 2 * standardDeviation)
        {
            // Always Healthy if the last pulse is within the average interval
            state = HealthStatus.Healthy;
            reason = "Pulse is occurring within the expected range.";
        }
        else if (elapsedSinceLastPulse <= averageInterval + 4 * standardDeviation)
        {
            // Degraded if the last pulse is slightly delayed
            state = HealthStatus.Degraded;
            reason = "Pulse frequency has slowed or become irregular.";
        }
        else
        {
            // Unhealthy if the last pulse is significantly delayed
            state = HealthStatus.Unhealthy;
            reason = "Pulse has significantly slowed or stopped.";
        }

        // Return status
        return new HealthComponent
        {
            Status = state,
            Details = new Dictionary<string, string>
            {
                { "message", reason },
                { "averageFrequency", $"{averageFrequency}" },
                { "averageInterval", $"{averagePulseInterval}" },
                { "maxInterval", $"{maxPulseInterval}" },
                { "standardDeviation", $"{standardDeviation}" },
                { "lastPulse", $"{lastPulse}" },
                { "nextExpectedPuse", $"{nextExpectedPuse}" },
                { "pulseCount", $"{_pulseTimes.Count}" },
            }
        };
    }

    private HealthComponent BuildPreHealthComponent()
    {
        var elapsedSinceLastPulse = _stopwatch.ElapsedMilliseconds - (_pulseTimes.LastOrDefault());

        if (_plannedInterval == null)
        {
            return new HealthComponent
            {
                Status = HealthStatus.Healthy,
                Details = new Dictionary<string, string>
                {
                    { "message", $"Not enough data to determine pulse status, assuming that the service is {HealthStatus.Degraded}." }
                }
            };
        }

        if (elapsedSinceLastPulse <= _plannedInterval.Value.TotalMilliseconds * 1.2)
        {
            return new HealthComponent
            {
                Status = HealthStatus.Healthy,
                Details = new Dictionary<string, string>
                {
                    { "message", $"Not enough data to determine pulse status, assuming that the service is {HealthStatus.Healthy}." }
                }
            };
        }

        if (elapsedSinceLastPulse <= _plannedInterval.Value.TotalMilliseconds * 1.8)
        {
            return new HealthComponent
            {
                Status = HealthStatus.Degraded,
                Details = new Dictionary<string, string>
                {
                    { "message", $"Not enough data to determine pulse status, assuming that the service is {HealthStatus.Degraded}. Taking over 20% longer than expected." }
                }
            };
        }

        return new HealthComponent
        {
            Status = HealthStatus.Unhealthy,
            Details = new Dictionary<string, string>
            {
                { "message", $"Not enough data to determine pulse status, assuming that the service is {HealthStatus.Unhealthy}. Taking over 80% longer than expected." }
            }
        };
    }
}