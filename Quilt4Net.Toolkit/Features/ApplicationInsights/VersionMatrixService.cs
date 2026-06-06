using System.Collections.Concurrent;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal sealed class VersionMatrixService : IVersionMatrixService
{
    // 1-hour TTL on the materialised view. The cells underneath have their own TtlCache lifetime
    // (also 1 hour); without this check the view's in-memory dictionary would serve stale data
    // indefinitely because the dict has no TTL of its own. On expiry we fall through to a fresh
    // RefreshAsync, which re-fetches via the cell cache (still warm if its TTL hasn't lapsed).
    private static readonly TimeSpan _viewTtl = TimeSpan.FromHours(1);

    private readonly IApplicationInsightsService _ai;
    private readonly ConcurrentDictionary<CacheKey, VersionMatrixView> _cache = new();

    public VersionMatrixService(IApplicationInsightsService ai)
    {
        _ai = ai;
    }

    public Task<VersionMatrixView> GetAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, CancellationToken cancellationToken = default)
    {
        var key = new CacheKey(context?.WorkspaceId ?? string.Empty, lookback);
        if (_cache.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.LastRefreshedUtc < _viewTtl)
        {
            return Task.FromResult(cached);
        }

        return RefreshAsync(context, lookback, cancellationToken);
    }

    public async Task<VersionMatrixView> RefreshAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, CancellationToken cancellationToken = default)
    {
        var cells = new List<VersionMatrixCell>();
        await foreach (var cell in _ai.GetVersionMatrixAsync(context, lookback).WithCancellation(cancellationToken))
        {
            cells.Add(cell);
        }

        var view = VersionMatrixView.FromCells(cells);

        var key = new CacheKey(context?.WorkspaceId ?? string.Empty, lookback);
        _cache[key] = view;
        return view;
    }

    private readonly record struct CacheKey(string WorkspaceId, TimeSpan? Lookback);
}
