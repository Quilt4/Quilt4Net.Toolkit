namespace Quilt4Net.Toolkit.Features.Api;

/// <summary>
/// Configuration options for the heartbeat background service.
/// Can be configured via code or appsettings.json at "Quilt4Net:HealthApi:Heartbeat".
/// </summary>
public record HeartbeatOptions
{
    /// <summary>
    /// Enable or disable the heartbeat background service.
    /// When enabled, availability telemetry is sent to Application Insights at the configured interval.
    /// Default is false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The interval between heartbeat executions.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional Application Insights connection string used when no TelemetryClient is registered via AddApplicationInsightsTelemetry.
    /// If a TelemetryClient is already registered in DI, that client is used and this value is ignored.
    /// If no TelemetryClient is registered and this value is not set, a warning is logged and heartbeat execution is skipped.
    /// </summary>
    public string ConnectionString { get; set; }
}