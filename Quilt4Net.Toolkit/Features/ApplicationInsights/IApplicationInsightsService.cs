namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsService
{
    Task<bool> CanConnectAsync(IApplicationInsightsContext context);

    /// <summary>
    /// Billed ingestion volume (MB) per source table over <paramref name="timeSpan"/>, from the
    /// workspace <c>Usage</c> table — billing-grade and cheap (no telemetry scan). Backs the logging
    /// cost dashboard's per-source breakdown. Returns empty when the <c>Usage</c> table can't be read
    /// (permissions) rather than throwing, so the view degrades gracefully.
    /// </summary>
    IAsyncEnumerable<VolumeBySource> GetVolumeBySourceAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);
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
    IAsyncEnumerable<VersionMatrixCell> GetVersionMatrixAsync(IApplicationInsightsContext context, TimeSpan? lookback = null, bool forceRefresh = false);

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
    /// Kubernetes node CPU-used <b>percentage</b> per node (<c>k8s.node.name</c>) over
    /// <paramref name="timeSpan"/>. Computed as <c>100 * k8s.node.cpu.usage / k8s.node.allocatable_cpu</c>
    /// (cores used / total schedulable cores; on k3s allocatable == physical cores). Only bins where
    /// <c>allocatable_cpu</c> is present render, so coverage starts when the collector began emitting
    /// it. <see cref="MetricSample.Series"/> is the node name. Bin size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterNodeCpuAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kubernetes node memory-used <b>percentage</b> per node (<c>k8s.node.name</c>) over
    /// <paramref name="timeSpan"/>. Computed per bin as
    /// <c>100 * usage / (usage + available)</c> from kubeletstats <c>k8s.node.memory.usage</c>
    /// and <c>k8s.node.memory.available</c> (there is no direct capacity metric; the pair sums to
    /// total). <see cref="MetricSample.Series"/> is the node name. Bin size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterNodeMemoryAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-pod CPU usage in <b>cores</b> for the pods scheduled on <paramref name="node"/>
    /// (<c>k8s.node.name</c>) over <paramref name="timeSpan"/>. Source: kubeletstats
    /// <c>k8s.pod.cpu.usage</c>. This is the node→pod drill-down for
    /// <see cref="GetClusterNodeCpuAsync"/>. <see cref="MetricSample.Series"/> is
    /// <c>{k8s.namespace.name}/{k8s.pod.name}</c>. Bin size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterPodCpuAsync(IApplicationInsightsContext context, string node, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-pod memory usage in <b>MB</b> for the pods scheduled on <paramref name="node"/>
    /// (<c>k8s.node.name</c>) over <paramref name="timeSpan"/>. Source: kubeletstats
    /// <c>k8s.pod.memory.usage</c> (absolute bytes — pods have no consistent capacity to render a
    /// percentage against, unlike nodes). This is the node→pod drill-down for
    /// <see cref="GetClusterNodeMemoryAsync"/>. <see cref="MetricSample.Series"/> is
    /// <c>{k8s.namespace.name}/{k8s.pod.name}</c>. Bin size scales with the window.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterPodMemoryAsync(IApplicationInsightsContext context, string node, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kubernetes node filesystem-used <b>percentage</b> per node (<c>k8s.node.name</c>) over
    /// <paramref name="timeSpan"/>. Computed as <c>100 * usage / capacity</c> from kubeletstats
    /// <c>k8s.node.filesystem.usage</c> and <c>k8s.node.filesystem.capacity</c> — answers "is a
    /// node running out of disk". <see cref="MetricSample.Series"/> is the node name. Bin size
    /// scales with the window. (Host disk I/O is intentionally not surfaced: the cluster Linux
    /// nodes' <c>system.disk.*</c> rows carry no host identity, so it cannot be attributed.)
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterNodeFilesystemAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whole-cluster CPU-used <b>percentage</b> over <paramref name="timeSpan"/>, capacity-weighted:
    /// <c>100 * Σ usage / Σ allocatable_cpu</c> across all nodes per bin (a 12-core node contributes
    /// proportionally more than a 2-core node). Source: <c>k8s.node.cpu.usage</c> /
    /// <c>k8s.node.allocatable_cpu</c>. <see cref="MetricSample.Series"/> is <c>"Cluster"</c>.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterTotalCpuAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whole-cluster total memory-used <b>percentage</b> over <paramref name="timeSpan"/>, weighted
    /// by capacity: <c>100 * Σ usage / Σ (usage + available)</c> across all nodes per bin (so larger
    /// nodes dominate the total). Source: <c>k8s.node.memory.usage</c> / <c>k8s.node.memory.available</c>.
    /// <see cref="MetricSample.Series"/> is <c>"Cluster"</c>.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterTotalMemoryAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whole-cluster total filesystem-used <b>percentage</b> over <paramref name="timeSpan"/>,
    /// weighted by capacity: <c>100 * Σ usage / Σ capacity</c> across all nodes per bin. Source:
    /// <c>k8s.node.filesystem.usage</c> / <c>k8s.node.filesystem.capacity</c>.
    /// <see cref="MetricSample.Series"/> is <c>"Cluster"</c>.
    /// </summary>
    IAsyncEnumerable<MetricSample> GetClusterTotalFilesystemAsync(IApplicationInsightsContext context, TimeSpan timeSpan, CancellationToken cancellationToken = default);

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