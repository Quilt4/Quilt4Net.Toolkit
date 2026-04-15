namespace Quilt4Net.Toolkit;

/// <summary>
/// Options for universal telemetry tagging.
/// Configurable via code or appsettings.json at "Quilt4Net:Logging".
/// </summary>
public record Quilt4NetLoggingOptions
{
    /// <summary>
    /// Application name used to identify this application in telemetry.
    /// Maps to Cloud.RoleName in Application Insights.
    /// Default is the entry assembly name.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Application version.
    /// Maps to Component.Version in Application Insights.
    /// Default is the entry assembly version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Environment name (e.g. Development, Staging, Production).
    /// Maps to GlobalProperties["Environment"] in Application Insights.
    /// Default resolution: IHostEnvironment.EnvironmentName → DOTNET_ENVIRONMENT → ASPNETCORE_ENVIRONMENT → "Production".
    /// </summary>
    public string Environment { get; set; }
}
