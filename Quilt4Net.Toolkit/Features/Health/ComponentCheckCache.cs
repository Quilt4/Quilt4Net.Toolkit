using System.Collections.Concurrent;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Outcome of running a single component check. Shared between <see cref="HealthService"/> and
/// <see cref="ComponentCheckCache"/> so cached results can be reused across health polls.
/// </summary>
internal record RunTaskResult
{
    public required string Name { get; init; }
    public required bool Essential { get; init; }
    public required CheckResult Result { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public Exception Exception { get; init; }
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// Singleton cache for deep <see cref="Component"/> check results, keyed by component name. Mirrors
/// the Dependency-probe caching added in #119: within a component's <see cref="Component.CacheDuration"/>
/// the last <see cref="RunTaskResult"/> is reused instead of re-running the check, and concurrent runs
/// for the same component are coalesced through a per-key gate so a burst of polls triggers a single
/// check. Lives as a singleton because <see cref="HealthService"/> is transient — the cache must
/// outlive a single request to be effective.
/// </summary>
internal sealed class ComponentCheckCache
{
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CacheSlot> _cache = new();

    public ComponentCheckCache(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Return a cached result for <paramref name="name"/> if one is still fresh for
    /// <paramref name="cacheDuration"/>; otherwise run <paramref name="run"/> once (coalescing
    /// concurrent callers) and cache it. When <paramref name="cacheDuration"/> is null or
    /// non-positive, caching is bypassed entirely.
    /// </summary>
    public async Task<RunTaskResult> GetOrRunAsync(string name, TimeSpan? cacheDuration, Func<Task<RunTaskResult>> run)
    {
        if (cacheDuration is not { } duration || duration <= TimeSpan.Zero)
        {
            return await run();
        }

        var slot = _cache.GetOrAdd(name, _ => new CacheSlot());

        if (TryGetFresh(slot, duration, out var cached))
        {
            return cached;
        }

        await slot.Gate.WaitAsync();
        try
        {
            if (TryGetFresh(slot, duration, out cached))
            {
                return cached;
            }

            var result = await run();
            slot.Result = result;
            slot.FetchedAt = _timeProvider.GetUtcNow();
            return result;
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private bool TryGetFresh(CacheSlot slot, TimeSpan cacheDuration, out RunTaskResult result)
    {
        if (slot.Result != null && _timeProvider.GetUtcNow() - slot.FetchedAt < cacheDuration)
        {
            result = slot.Result;
            return true;
        }

        result = null;
        return false;
    }

    private sealed class CacheSlot
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public RunTaskResult Result { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
    }
}
