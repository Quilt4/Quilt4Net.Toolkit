namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One point of a host-grouped time series, materialised from a <c>customMetrics</c> KQL query.
/// <para>
/// <see cref="Series"/> is the per-line legend label — typically <c>cloud_RoleInstance</c>, or
/// <c>{cloud_RoleInstance} {device}</c> for the disk metric where a single host can host several
/// filesystems. The value is in the metric's natural unit (CPU/memory: percent, disk: GB,
/// network: MB/s — see <see cref="IApplicationInsightsService"/>'s metric methods).
/// </para>
/// </summary>
/// <param name="Series">Legend / series label.</param>
/// <param name="Timestamp">Bin start (UTC).</param>
/// <param name="Value">Aggregated value for the bin, in the metric's natural unit.</param>
public record MetricSample(string Series, System.DateTime Timestamp, double Value);
