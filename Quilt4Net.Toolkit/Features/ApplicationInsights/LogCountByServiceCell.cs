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
/// <param name="Count">Retained row count in the lookback window for that (service, severity,
/// environment, source) cell — the number of records actually kept after ingestion sampling.</param>
/// <param name="Bytes">Retained billed bytes (<c>sum(_BilledSize)</c>) for the cell.</param>
/// <param name="Machine">Source host / role instance the rows came from.</param>
/// <param name="TrueCount">Sampling-corrected ("true") count: <c>sum(ItemCount)</c> — what the count
/// would have been with no ingestion sampling. Equals <see cref="Count"/> when the cell is unsampled
/// (every retained row represents itself, <c>ItemCount == 1</c>).</param>
/// <param name="TrueBytes">Sampling-corrected ("true") billed bytes: <c>sum(_BilledSize * ItemCount)</c>
/// — the estimated volume had nothing been sampled out. Equals <see cref="Bytes"/> when unsampled.</param>
public record LogCountByServiceCell(
    string Service,
    SeverityLevel Severity,
    string Environment,
    LogSource Source,
    long Count,
    long Bytes = 0,
    string Machine = "",
    long TrueCount = 0,
    long TrueBytes = 0);
