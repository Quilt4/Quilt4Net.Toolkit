namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One bucket of a service × severity log-count matrix. The flat shape lets the Blazor side pivot
/// rows by <see cref="Service"/> and columns by <see cref="Severity"/> in a single grid; rows
/// missing a given severity simply mean zero counts for that combination.
/// </summary>
/// <param name="Service">Source application — coalesced from <c>Properties.ApplicationName</c> then
/// <c>AppRoleName</c>, falling back to <c>"unknown"</c>.</param>
/// <param name="Severity">Effective severity, unified across log sources:
/// <see cref="SeverityLevel.Information"/> for successful requests, <see cref="SeverityLevel.Error"/>
/// for failed requests or unhandled exceptions, otherwise the row's own <c>SeverityLevel</c>.</param>
/// <param name="Count">Row count in the lookback window for that (service, severity) cell.</param>
public record LogCountByServiceCell(string Service, SeverityLevel Severity, long Count);
