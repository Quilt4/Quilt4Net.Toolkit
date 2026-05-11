using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Logging;

internal static class Quilt4NetStartupLogger
{
    /// <summary>
    /// Property name used to identify the Quilt4Net startup log entry. Emitted as a
    /// **structured argument** on the message template (not via <c>BeginScope</c>) so it
    /// lands in <see cref="OpenTelemetry.Logs.LogRecord.Attributes"/> regardless of the
    /// consumer's <c>OpenTelemetryLoggerOptions.IncludeScopes</c> setting. The Azure
    /// Monitor OpenTelemetry exporter defaults that to <c>false</c>, so a previous
    /// <c>BeginScope</c>-based implementation never reached App Insights and the
    /// VersionMatrix "Startup" fast path was unreachable in practice.
    /// </summary>
    public const string StartupPropertyName = "Quilt4NetStartup";

    public static void Log(ILogger logger, Quilt4NetLoggingOptions options)
    {
        // ServiceInstanceId is appended in square brackets between the application name and
        // version when set, so multi-deployment hosts can be told apart at-a-glance in the
        // startup line. Format collapses cleanly to the historical shape when unset.
        if (!string.IsNullOrEmpty(options.ServiceInstanceId))
        {
            logger.LogInformation(
                "Quilt4Net startup: {ApplicationName} [{ServiceInstanceId}] v{Version} in {Environment} ({" + StartupPropertyName + "})",
                options.ApplicationName ?? "(unknown)",
                options.ServiceInstanceId,
                options.Version ?? "(unknown)",
                options.Environment ?? "(unknown)",
                "true");
        }
        else
        {
            logger.LogInformation(
                "Quilt4Net startup: {ApplicationName} v{Version} in {Environment} ({" + StartupPropertyName + "})",
                options.ApplicationName ?? "(unknown)",
                options.Version ?? "(unknown)",
                options.Environment ?? "(unknown)",
                "true");
        }
    }
}
