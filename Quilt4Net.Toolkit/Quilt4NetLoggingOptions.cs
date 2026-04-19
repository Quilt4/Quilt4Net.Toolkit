namespace Quilt4Net.Toolkit;

/// <summary>
/// Options for universal telemetry identity.
/// Configurable via code or appsettings.json at "Quilt4Net:Logging".
/// These values populate OpenTelemetry Resource attributes and are attached to traces, logs and metrics automatically.
/// </summary>
public record Quilt4NetLoggingOptions
{
    /// <summary>
    /// Application name used to identify this application in telemetry.
    /// Maps to OpenTelemetry resource attribute <c>service.name</c>
    /// (surfaces as <c>cloud_RoleName</c> in Application Insights).
    /// Default is the entry assembly name.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Application version.
    /// Maps to OpenTelemetry resource attribute <c>service.version</c>
    /// (surfaces as <c>application_Version</c> in Application Insights).
    /// Default is the entry assembly version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Environment name (e.g. Development, Staging, Production).
    /// Maps to OpenTelemetry resource attribute <c>deployment.environment</c>
    /// (surfaces under <c>customDimensions</c> in Application Insights).
    /// Default resolution: IHostEnvironment.EnvironmentName → DOTNET_ENVIRONMENT → ASPNETCORE_ENVIRONMENT → "Production".
    /// </summary>
    public string Environment { get; set; }
}
