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

        // Re-key the per-machine breakdown under logical names too — without this, the "Per machine"
        // UI toggle would still find apps under their raw source names and the alias-folded grid
        // wouldn't expand. For each (logical, env) we union the machine rows from every source name
        // and dedupe by machine, keeping the latest-LastSeen winner per machine.
        var newCellsByMachine = new Dictionary<(string App, string Env), IReadOnlyList<VersionMatrixCell>>();
        if (raw.CellsByMachine is { Count: > 0 })
        {
            var groupedByMachine = raw.CellsByMachine
                .SelectMany(kvp => kvp.Value.Select(c => new
                {
                    Logical = sourceToLogical.GetValueOrDefault(c.ApplicationName, c.ApplicationName),
                    Env = c.Environment,
                    Cell = c
                }))
                .GroupBy(x => (App: x.Logical, x.Env));
            foreach (var g in groupedByMachine)
            {
                var machines = g
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.Cell.Machine) ? VersionMatrixView.UnknownMachine : x.Cell.Machine,
                             StringComparer.OrdinalIgnoreCase)
                    .Select(mg => mg
                        .OrderByDescending(x => x.Cell.LastSeen)
                        .First().Cell with { ApplicationName = g.Key.App })
                    .OrderBy(c => c.Machine, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                newCellsByMachine[g.Key] = machines;
            }
        }

        return raw with
        {
            Applications = newApps,
            Cells = newCells,
            CellsByMachine = newCellsByMachine,
            CellAliases = newAliases
        };
    }
}
