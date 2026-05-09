using System.Collections.Concurrent;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal sealed class VersionMatrixService : IVersionMatrixService
{
    private readonly IApplicationInsightsService _ai;
    private readonly ConcurrentDictionary<CacheKey, VersionMatrixView> _cache = new();

    public VersionMatrixService(IApplicationInsightsService ai)
    {
        _ai = ai;
    }

    public Task<VersionMatrixView> GetAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, CancellationToken cancellationToken = default)
    {
        var key = new CacheKey(context?.WorkspaceId ?? string.Empty, lookback);
        if (_cache.TryGetValue(key, out var cached))
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
