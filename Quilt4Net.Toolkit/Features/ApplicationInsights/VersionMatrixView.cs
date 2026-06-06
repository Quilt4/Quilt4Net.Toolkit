namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Materialized application/environment version matrix ready for UI rendering.
/// </summary>
public sealed record VersionMatrixView
{
    internal const string UnknownEnvironment = "(unknown)";

    /// <summary>Application names sorted alphabetically.</summary>
    public required IReadOnlyList<string> Applications { get; init; }

    /// <summary>Environment names sorted by standard order (Development → Staging → Test → Production → other → unknown).</summary>
    public required IReadOnlyList<string> Environments { get; init; }

    /// <summary>Cells keyed by (ApplicationName, Environment). Missing keys = empty cell.</summary>
    public required IReadOnlyDictionary<(string App, string Env), VersionMatrixCell> Cells { get; init; }

    /// <summary>
    /// Per-machine breakdown of the same data: for each (App, Env), the list of cells observed
    /// on each individual machine running that app. Populated when the underlying telemetry
    /// carries a machine identifier (host.name or AppRoleInstance); empty list otherwise. The
    /// UI's "Per machine" toggle reads from this map to expand an app row into one sub-row per
    /// machine. The aggregated <see cref="Cells"/> still picks the latest version across machines
    /// for the default app-level view.
    /// </summary>
    public IReadOnlyDictionary<(string App, string Env), IReadOnlyList<VersionMatrixCell>> CellsByMachine { get; init; }
        = new Dictionary<(string, string), IReadOnlyList<VersionMatrixCell>>();

    /// <summary>UTC timestamp the data was fetched from Application Insights.</summary>
    public required DateTime LastRefreshedUtc { get; init; }

    /// <summary>
    /// Per-cell alias info, populated only when an alias map has been applied
    /// (typically by a host that provides an <c>AliasFolder</c> delegate to
    /// <c>VersionMatrixDisplay</c>). Empty by default — consumers that don't
    /// have an alias concept can ignore this entirely.
    /// </summary>
    public IReadOnlyDictionary<(string App, string Env), CellAlias> CellAliases { get; init; }
        = new Dictionary<(string, string), CellAlias>();

    public bool TryGetCell(string app, string env, out VersionMatrixCell cell) => Cells.TryGetValue((app, env), out cell);
    public bool TryGetAlias(string app, string env, out CellAlias info) => CellAliases.TryGetValue((app, env), out info);

    /// <summary>
    /// Build a view from a flat list of cells. Environments are returned alphabetical;
    /// final domain ordering (Development → Staging → Test → Production, then unknown) is
    /// applied at render time by <c>VersionMatrixDisplay</c>, so the same fetched view can
    /// be reused under different ordering preferences.
    /// </summary>
    /// <param name="lastRefreshedUtc">
    /// Original load timestamp to stamp on the returned view. Pass the oldest per-context
    /// <see cref="LastRefreshedUtc"/> when merging multiple workspace views so the
    /// "Data loaded at …" indicator reflects the real fetch time across cache hits, not the
    /// moment this merge happened to run. <c>null</c> stamps <see cref="DateTime.UtcNow"/>.
    /// </param>
    public static VersionMatrixView FromCells(IReadOnlyList<VersionMatrixCell> cells, DateTime? lastRefreshedUtc = null)
    {
        var apps = cells
            .Select(c => c.ApplicationName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var envs = cells
            .Select(c => string.IsNullOrWhiteSpace(c.Environment) ? UnknownEnvironment : c.Environment)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Normalise environment once so both the aggregated map and the per-machine map key on the
        // same string. Without this, "" and "(unknown)" would split into two separate buckets.
        var normalised = cells
            .Select(c => c with { Environment = string.IsNullOrWhiteSpace(c.Environment) ? UnknownEnvironment : c.Environment })
            .ToList();

        // Aggregated cell — one per (App, Env), picking the latest-by-LastSeen across all machines.
        // Machine identity is dropped so consumers that don't care about per-machine breakdown can
        // read the cell at face value.
        var cellMap = normalised
            .GroupBy(c => (App: c.ApplicationName, Env: c.Environment))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.LastSeen).First() with { Machine = "" });

        // Per-machine map — for each (App, Env), one cell per machine that's reported a version.
        // If two rows arrive for the same (App, Env, Machine) we pick the latest. Each list is
        // ordered alphabetically by machine name so the UI's sub-rows render in a stable order.
        var byMachine = normalised
            .GroupBy(c => (App: c.ApplicationName, Env: c.Environment))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<VersionMatrixCell>)g
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.Machine) ? UnknownMachine : c.Machine)
                    .Select(mg => mg.OrderByDescending(c => c.LastSeen).First() with
                    {
                        Machine = string.IsNullOrWhiteSpace(mg.Key) ? UnknownMachine : mg.Key
                    })
                    .OrderBy(c => c.Machine, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        return new VersionMatrixView
        {
            Applications = apps,
            Environments = envs,
            Cells = cellMap,
            CellsByMachine = byMachine,
            LastRefreshedUtc = lastRefreshedUtc ?? DateTime.UtcNow
        };
    }

    /// <summary>Sentinel used when a cell's <c>Machine</c> field is empty.</summary>
    internal const string UnknownMachine = "(unknown)";
}

/// <summary>
/// Detail about how a folded cell was constructed from underlying raw cells.
/// </summary>
public sealed record CellAlias
{
    /// <summary>The raw application names that contributed to this cell, sorted alphabetically.</summary>
    public required IReadOnlyList<string> SourceNames { get; init; }

    /// <summary>
    /// When 2+ source apps in this (LogicalApp, Environment) reported different versions,
    /// each reporting source is listed here with its version + last-seen timestamp.
    /// Empty when all sources agreed (or only one source contributed).
    /// </summary>
    public IReadOnlyList<CellAliasSource> ConflictingVersions { get; init; } = [];

    public bool HasConflict => ConflictingVersions.Count > 1;
}

public sealed record CellAliasSource(string SourceName, string Version, DateTime LastSeen);
