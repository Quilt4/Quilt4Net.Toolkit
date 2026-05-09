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
    public static VersionMatrixView FromCells(IReadOnlyList<VersionMatrixCell> cells)
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

        var cellMap = cells
            .GroupBy(c => (App: c.ApplicationName, Env: string.IsNullOrWhiteSpace(c.Environment) ? UnknownEnvironment : c.Environment))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.LastSeen).First());

        return new VersionMatrixView
        {
            Applications = apps,
            Environments = envs,
            Cells = cellMap,
            LastRefreshedUtc = DateTime.UtcNow
        };
    }
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
