using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Features.Health.Dependency;

internal class DependencyService : IDependencyService
{
    private readonly IDependencyProbe _probe;
    private readonly Quilt4NetHealthApiOptions _apiOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CacheSlot> _cache = new();

    public DependencyService(IDependencyProbe probe, Quilt4NetHealthApiOptions apiOptions, TimeProvider timeProvider)
    {
        _probe = probe;
        _apiOptions = apiOptions;
        _timeProvider = timeProvider;
    }

    public async IAsyncEnumerable<KeyValuePair<string, DependencyComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = _apiOptions.DependencyRegistrations
            .Select(x => GetComponentAsync(x, cancellationToken))
            .ToList();

        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks);
            tasks.Remove(task);
            yield return await task;
        }
    }

    private async Task<KeyValuePair<string, DependencyComponent>> GetComponentAsync(Dependency dependency, CancellationToken cancellationToken)
    {
        var content = await GetContentAsync(dependency, cancellationToken);

        var component = new DependencyComponent
        {
            Status = BuildStatus(dependency.Essential, content.Components),
            Uri = dependency.Uri,
            DependencyComponents = content.Components
        };

        return new KeyValuePair<string, DependencyComponent>(dependency.Name, component);
    }

    private async Task<HealthResponse> GetContentAsync(Dependency dependency, CancellationToken cancellationToken)
    {
        var cacheTime = _apiOptions.DependencyProbeCacheTime;
        if (cacheTime <= TimeSpan.Zero)
        {
            return await _probe.ProbeAsync(dependency, cancellationToken);
        }

        var slot = _cache.GetOrAdd(dependency.Name, _ => new CacheSlot());

        if (TryGetFresh(slot, cacheTime, out var cached))
        {
            return cached;
        }

        await slot.Gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetFresh(slot, cacheTime, out cached))
            {
                return cached;
            }

            var content = await _probe.ProbeAsync(dependency, cancellationToken);
            slot.Content = content;
            slot.FetchedAt = _timeProvider.GetUtcNow();
            return content;
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private bool TryGetFresh(CacheSlot slot, TimeSpan cacheTime, out HealthResponse content)
    {
        if (slot.Content != null && _timeProvider.GetUtcNow() - slot.FetchedAt < cacheTime)
        {
            content = slot.Content;
            return true;
        }

        content = null;
        return false;
    }

    private static HealthStatus BuildStatus(bool essential, Dictionary<string, HealthComponent> components)
    {
        var status = components.Count == 0 ? HealthStatus.Healthy : components.Max(x => x.Value.Status);
        if (!essential && status == HealthStatus.Unhealthy)
        {
            status = HealthStatus.Degraded;
        }

        return status;
    }

    private sealed class CacheSlot
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public HealthResponse Content { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
    }
}
