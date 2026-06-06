namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One bucket of the (service × severity × environment × source) log-count cube. The shape carries
/// every dimension the admin UI filters on so a single server fetch covers any future combination
/// of configuration / environment / source toggles — flipping a checkbox is then a pure local
/// regroup with no extra Log Analytics queries.
/// </summary>
/// <param name="Service">Source application — coalesced from <c>Properties.ApplicationName</c> then
/// <c>AppRoleName</c>, falling back to <c>"unknown"</c>.</param>
/// <param name="Severity">Effective severity, unified across log sources:
/// <see cref="SeverityLevel.Information"/> for successful requests, <see cref="SeverityLevel.Error"/>
/// for failed requests or unhandled exceptions, otherwise the row's own <c>SeverityLevel</c>.</param>
/// <param name="Environment">Environment name as projected by the Quilt4Net standard projection
/// (<c>deployment.environment</c> → <c>AspNetCoreEnvironment</c> fallback). Empty when neither tag
/// was logged.</param>
/// <param name="Source">Which AI table the row came from — Trace / Exception / Request.</param>
/// <param name="Count">Row count in the lookback window for that (service, severity, environment,
/// source) cell.</param>
public record LogCountByServiceCell(
    string Service,
    SeverityLevel Severity,
    string Environment,
    LogSource Source,
    long Count,
    string Machine = "");
