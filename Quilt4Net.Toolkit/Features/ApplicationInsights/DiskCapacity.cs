namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Per-volume disk capacity snapshot derived from <c>system.filesystem.usage</c> AppMetrics. The
/// free/used/reserved time-series is exposed separately via
/// <see cref="IApplicationInsightsService.GetDiskFreeAsync"/>; this record carries the constant
/// "how big is this filesystem" context that lets a UI render free space as a percentage of
/// capacity instead of scaling bars relative to whichever host happens to have the most space.
/// </summary>
/// <param name="Series">
/// Same series key as the corresponding <see cref="MetricSample.Series"/> from
/// <see cref="IApplicationInsightsService.GetDiskFreeAsync"/> — typically <c>{host} {device}</c>.
/// </param>
/// <param name="FreeGb">Average free space over the queried window, in GB.</param>
/// <param name="UsedGb">Average used space over the queried window, in GB.</param>
/// <param name="ReservedGb">
/// Average reserved (e.g. filesystem-reserved blocks not counted as used or free) space over
/// the window, in GB. Often zero on Windows filesystems.
/// </param>
/// <param name="TotalGb">
/// <see cref="FreeGb"/> + <see cref="UsedGb"/> + <see cref="ReservedGb"/> — the volume's
/// total capacity as observed by the OS. Computed server-side so the consumer doesn't need
/// to re-sum and can rely on whatever rounding the KQL emits.
/// </param>
public record DiskCapacity(string Series, double FreeGb, double UsedGb, double ReservedGb, double TotalGb);
