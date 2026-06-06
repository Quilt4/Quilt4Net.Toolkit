namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    Task<bool> CanConnectAsync(IApplicationInsightsContext context);
    IAsyncEnumerable<EnvironmentOption> GetEnvironments(IApplicationInsightsContext context);
    IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, CancellationToken cancellationToken = default);
    IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, CancellationToken cancellationToken = default);
    Task<LogDetails> GetDetail(IApplicationInsightsContext context, string id, LogSource source, string environment, TimeSpan timeSpan);
    /// <summary>
    /// Get the latest items for a single fingerprint. Capped at <paramref name="maxItems"/>
    /// (default 100) — a fingerprint can match 100k+ rows over the lookback window, and
    /// returning all of them is what made detail/summary navigation hang. Items are ordered
    /// newest-first; only the top <paramref name="maxItems"/> are returned.
    /// </summary>
    Task<SummaryData> GetSummary(IApplicationInsightsContext context, string fingerprint, LogSource source, string environment, TimeSpan timeSpan, int maxItems = 100);
    IAsyncEnumerable<SummarySubset> GetSummaries(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest version of each (application, environment) combination
    /// seen in the workspace. Tries the Quilt4NetStartup-tagged fast path
    /// first, then falls back to a general scan for cells the fast path
    /// did not cover. Pass <c>null</c> for <paramref name="lookback"/> to
    /// query the workspace's full retention.
    /// </summary>
    IAsyncEnumerable<VersionMatrixCell> GetVersionMatrixAsync(IApplicationInsightsContext context, TimeSpan? lookback = null);

    /// <summary>
    /// Returns all telemetry rows whose <c>Properties.IncidentId</c> matches
    /// <paramref name="incidentId"/>. Searches AppTraces, AppExceptions, and
    /// AppRequests over the supplied lookback window. Used by the MCP
    /// <c>quilt4net.lookup_incident</c> tool to map a user-facing incident id
    /// (e.g. from a UI alert) back to the structured log entry that produced it.
    /// </summary>
    IAsyncEnumerable<LogItem> SearchByIncidentIdAsync(IApplicationInsightsContext context, string incidentId, TimeSpan timeSpan);

    /// <summary>
    /// Returns all telemetry rows whose <c>OperationId</c> or
    /// <c>Properties.CorrelationId</c> matches <paramref name="correlationId"/>.
    /// Searches AppTraces, AppExceptions, and AppRequests over the supplied
    /// lookback window.
    /// </summary>
    IAsyncEnumerable<LogItem> SearchByCorrelationIdAsync(IApplicationInsightsContext context, string correlationId, TimeSpan timeSpan);

    /// <summary>
    /// CPU-busy percentage per host (<c>cloud_RoleInstance</c>) over <paramref name="timeSpan"/>.
    /// Derived from the OpenTelemetry <c>system.cpu.utilization</c> idle reading:
    /// <c>100 * (1 - avg(idle))</c>. Bin size scales with the window (see <see cref="MetricsBinSelector"/>).
    /// </summary>
    IAsyncEnumerable<MetricSample> GetCpuUtilizationAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Memory-used percentage per host (<c>cloud_RoleInstance</c>) over <paramref name="timeSpan"/>.
    /// Source: OpenTelemetry <c>system.memory.utilization</c> with state=used. Bin size scales with
    /// the window (see <see cref="MetricsBinSelector"/>).
    /// </summary>
    IAsyncEnumerable<MetricSample> GetMemoryUtilizationAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filesystem free (GB) per host+device over <paramref name="timeSpan"/>. Source: OpenTelemetry
    /// <c>system.filesystem.usage</c> with state=free — operators want the "how much room is left"
    /// view, where a line trending toward zero means the disk is filling up. Series label is
    /// <c>{host.name} {device}</c> so multiple volumes on one host show as separate lines. Bin
    /// size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetDiskFreeAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-volume disk capacity (total GB, plus the free/used/reserved breakdown) across
    /// <paramref name="timeSpan"/>. Sourced from <c>system.filesystem.usage</c> by summing the
    /// three states per volume — gives the UI a real "% of capacity" reference for free-space bars
    /// instead of needing to scale them relative to whichever host happens to have the most space.
    /// Series label matches <see cref="GetDiskFreeAsync"/> so consumers can join the two by
    /// <see cref="MetricSample.Series"/> / <see cref="DiskCapacity.Series"/>.
    /// </summary>
    IAsyncEnumerable<DiskCapacity> GetDiskCapacityAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Network throughput (MB/s) per host (<c>cloud_RoleInstance</c>) over <paramref name="timeSpan"/>.
    /// Computed as the per-bin delta of <c>system.network.io</c> divided by the bin duration; negative
    /// deltas (host restart / counter reset) are dropped. Bin size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetNetworkThroughputAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts log entries per (service × severity × environment × source) across <c>AppTraces</c>,
    /// <c>AppExceptions</c> and <c>AppRequests</c> over <paramref name="timeSpan"/>. Severity is
    /// unified across sources: traces use their own <c>SeverityLevel</c>; exceptions are always
    /// counted as <see cref="SeverityLevel.Error"/>; requests use Information when Success is true
    /// and Error otherwise. Service is coalesced from <c>Properties.ApplicationName</c> then
    /// <c>AppRoleName</c>.
    /// <para>
    /// No server-side env / source filter — admin UIs typically want every dimension at hand so
    /// flipping a filter checkbox can be done locally without an extra round trip. Pre-filtering
    /// is one <c>.Where</c> away at the call site.
    /// </para>
    /// </summary>
    IAsyncEnumerable<LogCountByServiceCell> GetLogCountByServiceAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);
}