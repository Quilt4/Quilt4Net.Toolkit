namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Folds a raw <see cref="VersionMatrixView"/> using a static <see cref="ApplicationAliasMap"/>
/// list (typically from <see cref="ApplicationInsightsOptions.ApplicationAlias"/>). Same
/// (logical, env) merge semantics as a dynamic per-team folder: cells whose ApplicationName
/// matches a known source name are merged under the logical name; for groups with disagreeing
/// versions the latest by LastSeen wins and the disagreement surfaces via
/// <see cref="CellAlias.ConflictingVersions"/>.
/// </summary>
public static class StaticAliasFolder
{
    public static VersionMatrixView Fold(VersionMatrixView raw, IReadOnlyList<ApplicationAliasMap> aliases)
    {
        if (aliases is null || aliases.Count == 0) return raw;

        var sourceToLogical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliases)
        {
            foreach (var src in alias.SourceNames ?? [])
            {
                if (string.IsNullOrWhiteSpace(src)) continue;
                sourceToLogical[src] = alias.LogicalName;
            }
        }

        if (sourceToLogical.Count == 0) return raw;

        var grouped = raw.Cells.Values
            .Select(c => new { Logical = sourceToLogical.GetValueOrDefault(c.ApplicationName, c.ApplicationName), Cell = c })
            .GroupBy(x => (App: x.Logical, Env: x.Cell.Environment), x => x.Cell);

        var newCells = new Dictionary<(string App, string Env), VersionMatrixCell>();
        var newAliases = new Dictionary<(string App, string Env), CellAlias>();

        foreach (var g in grouped)
        {
            var key = g.Key;
            var members = g.ToList();
            var winner = members.OrderByDescending(c => c.LastSeen).First();
            newCells[key] = winner with { ApplicationName = key.App };

            var sourceNames = members
                .Select(c => c.ApplicationName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var aliasApplied = sourceNames.Length > 1
                || !string.Equals(sourceNames[0], key.App, StringComparison.OrdinalIgnoreCase);
            if (!aliasApplied) continue;

            var distinctVersions = members.Select(c => c.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var conflicts = distinctVersions > 1
                ? members
                    .OrderByDescending(c => c.LastSeen)
                    .Select(c => new CellAliasSource(c.ApplicationName, c.Version, c.LastSeen))
                    .ToArray()
                : [];

            newAliases[key] = new CellAlias
            {
                SourceNames = sourceNames,
                ConflictingVersions = conflicts
            };
        }

        var newApps = newCells.Keys.Select(k => k.App).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return raw with
        {
            Applications = newApps,
            Cells = newCells,
            CellAliases = newAliases
        };
    }
}
