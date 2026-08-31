using System.Collections.Concurrent;

namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// How long to stop calling for a key after a failed call, widening with each consecutive failure
/// and resetting on the first success. Shared by the content and remote-configuration clients so
/// the two behave identically under a fault.
/// </summary>
/// <remarks>
/// <para>
/// Issue #174: both clients used to re-stamp a <b>full cache lifetime</b> on failure, so every
/// expiry landed on one failed call that bought another whole TTL of the fallback value. There was
/// no state in which a key converged on the server value while calls kept failing.
/// </para>
/// <para>
/// The base interval is therefore short — seconds, not the content lifetime — and doubles per
/// consecutive failure up to a ceiling, so a brief blip costs one slow read while a sustained
/// outage still settles into a low request rate instead of a per-render flood.
/// </para>
/// </remarks>
internal sealed class FailureBackoff
{
    private readonly ConcurrentDictionary<string, int> _consecutiveFailures = new();

    /// <summary>
    /// How many keys are currently in a failure streak, i.e. being held off. Published as a metric so
    /// a client backing off is visible directly rather than inferred from gaps between attempts.
    /// </summary>
    public int ActiveCount => _consecutiveFailures.Count;

    /// <summary>
    /// Records a failure for <paramref name="key"/> and returns how long to hold off before the
    /// next attempt: <paramref name="baseDuration"/> doubled per consecutive failure, capped at
    /// <paramref name="maxDuration"/>.
    /// </summary>
    public TimeSpan Next(string key, TimeSpan baseDuration, TimeSpan maxDuration)
    {
        var failures = _consecutiveFailures.AddOrUpdate(key, 1, (_, current) => current == int.MaxValue ? current : current + 1);
        return Compute(failures, baseDuration, maxDuration);
    }

    /// <summary>
    /// Clears the failure streak for <paramref name="key"/>, so the next failure starts again at
    /// <c>baseDuration</c> rather than at the widened interval the outage had reached.
    /// </summary>
    public void Reset(string key)
    {
        if (_consecutiveFailures.IsEmpty) return;
        _consecutiveFailures.TryRemove(key, out _);
    }

    /// <summary>
    /// The hold-off for the <paramref name="failures"/>-th consecutive failure. Exposed separately
    /// so the doubling is testable without driving a client through a real fault.
    /// </summary>
    internal static TimeSpan Compute(int failures, TimeSpan baseDuration, TimeSpan maxDuration)
    {
        if (baseDuration <= TimeSpan.Zero) return TimeSpan.Zero;
        if (maxDuration < baseDuration) return baseDuration;

        // Shift rather than Math.Pow, and stop shifting well before the exponent could overflow the
        // multiplication — anything past the cap is the cap regardless.
        var doublings = Math.Min(Math.Max(failures - 1, 0), 30);
        var scaled = baseDuration.Ticks * (1L << doublings);
        return scaled >= maxDuration.Ticks || scaled < 0 ? maxDuration : TimeSpan.FromTicks(scaled);
    }
}
