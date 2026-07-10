using Quilt4Net.Toolkit.Features.Health;
using System.Diagnostics;

namespace Quilt4Net.Toolkit.Features.Probe;

internal class HostedServiceProbe<TComponent> : HostedServiceProbe, IHostedServiceProbe<TComponent>
{
    public HostedServiceProbe(IHostedServiceProbeRegistry hostedServiceProbeRegistry)
        : base(hostedServiceProbeRegistry)
    {
    }

    public IHostedServiceProbe Register(TimeSpan? plannedInterval = null, bool autoMaxInterval = true, int pulseWindowSize = 100)
    {
        return Register(Name, plannedInterval, autoMaxInterval, pulseWindowSize);
    }

    public override string Name => typeof(TComponent).Name;
}

internal class HostedServiceProbe : IHostedServiceProbe
{
    // Rolling window of the most recent pulse timestamps (ms since the stopwatch started).
    // Bounded to _pulseWindowSize so memory and per-health-call cost stay constant regardless
    // of process uptime or pulse frequency. See issue #130 (unbounded growth -> OutOfMemoryException).
    private readonly Queue<long> _pulseTimes = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly object _gate = new();
    private long _pulseCount;
    private int _pulseWindowSize = 100;
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
        lock (_gate)
        {
            // Start the measurement at the first pulse.
            if (_isFirstPulse)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
                _isFirstPulse = false;
            }

            // Record when this pulse happened and trim to the rolling window.
            _pulseTimes.Enqueue(_stopwatch.ElapsedMilliseconds);
            while (_pulseTimes.Count > _pulseWindowSize)
            {
                _pulseTimes.Dequeue();
            }

            // Total count is tracked separately so pulseCount stays meaningful without retaining data.
            _pulseCount++;

            //NOTE: Reset end/exception if restarted.
            _ended = false;
            _exception = null;
        }
    }

    public IHostedServiceProbe Register(string name, TimeSpan? plannedInterval = null, bool autoMaxInterval = true, int pulseWindowSize = 100)
    {
        _name = name;
        _plannedInterval = plannedInterval;
        _autoMaxInterval = autoMaxInterval;
        lock (_gate)
        {
            // Need at least two timestamps to form an interval.
            _pulseWindowSize = Math.Max(2, pulseWindowSize);
            while (_pulseTimes.Count > _pulseWindowSize)
            {
                _pulseTimes.Dequeue();
            }
        }
        return this;
    }

    public void EndService(bool success)
    {
        lock (_gate)
        {
            if (!success) _exception = new Exception("Unknown");
            _ended = true;
        }
    }

    public void EndService(Exception exception)
    {
        lock (_gate)
        {
            _exception = exception;
            _ended = true;
        }
    }

    public HealthComponent GetHealth()
    {
        // Take a consistent snapshot under the lock, then compute outside it. The snapshot is
        // bounded by _pulseWindowSize, so this allocates and enumerates a constant amount of work.
        bool ended;
        Exception exception;
        long[] pulseTimes;
        long totalPulseCount;
        long nowElapsed;
        TimeSpan? plannedInterval;
        bool autoMaxInterval;
        lock (_gate)
        {
            ended = _ended;
            exception = _exception;
            pulseTimes = _pulseTimes.ToArray();
            totalPulseCount = _pulseCount;
            nowElapsed = _stopwatch.ElapsedMilliseconds;
            plannedInterval = _plannedInterval;
            autoMaxInterval = _autoMaxInterval;
        }

        if (ended)
        {
            var message = exception == null ? "Ended successfully." : $"Ended with exception. {exception.Message}";
            return new HealthComponent
            {
                Status = exception == null ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Details = new Dictionary<string, string>
                {
                    { "message", message }
                }
            };
        }

        if (pulseTimes.Length < 2)
        {
            var elapsed = nowElapsed - (pulseTimes.Length > 0 ? pulseTimes[^1] : 0);
            return BuildPreHealthComponent(elapsed, plannedInterval);
        }

        // Calculate intervals between pulses (over the bounded window).
        var intervals = new long[pulseTimes.Length - 1];
        for (var i = 1; i < pulseTimes.Length; i++)
        {
            intervals[i - 1] = pulseTimes[i] - pulseTimes[i - 1];
        }

        var averageInterval = intervals.Average();
        var variance = intervals.Select(interval => Math.Pow(interval - averageInterval, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);

        // Time since last pulse
        var elapsedSinceLastPulse = nowElapsed - pulseTimes[^1];

        //Extra
        var averageFrequency = 1000 / averageInterval;
        var averagePulseInterval = TimeSpan.FromMilliseconds(averageInterval);
        var maxPulseInterval = TimeSpan.FromMilliseconds(intervals.Max());
        var lastPulse = TimeSpan.FromMilliseconds(elapsedSinceLastPulse);
        var nextExpectedPuse = TimeSpan.FromMilliseconds(averageInterval - elapsedSinceLastPulse);

        // Determine state
        HealthStatus state;
        string reason;

        // Logic for determining status
        if (plannedInterval.HasValue && elapsedSinceLastPulse < plannedInterval.Value.TotalMilliseconds)
        {
            //Never report issue if the planned interval has not been reached.
            state = HealthStatus.Healthy;
            reason = "Pulse have not reached planned interval.";
        }
        else if (autoMaxInterval && elapsedSinceLastPulse < maxPulseInterval.TotalMilliseconds)
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
                { "pulseCount", $"{totalPulseCount}" },
            }
        };
    }

    private HealthComponent BuildPreHealthComponent(long elapsedSinceLastPulse, TimeSpan? plannedInterval)
    {
        if (plannedInterval == null)
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

        if (elapsedSinceLastPulse <= plannedInterval.Value.TotalMilliseconds * 1.2)
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

        if (elapsedSinceLastPulse <= plannedInterval.Value.TotalMilliseconds * 1.8)
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
