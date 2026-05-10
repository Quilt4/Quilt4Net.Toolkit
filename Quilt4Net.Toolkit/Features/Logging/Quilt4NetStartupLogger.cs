using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Logging;

internal static class Quilt4NetStartupLogger
{
    public static void Log(ILogger logger, Quilt4NetLoggingOptions options)
    {
        using (logger.BeginScope(new Dictionary<string, object> { ["Quilt4NetStartup"] = "true" }))
        {
            // ServiceInstanceId is appended in square brackets between the application name and
            // version when set, so multi-deployment hosts can be told apart at-a-glance in the
            // startup line. Format collapses cleanly to the historical shape when unset.
            if (!string.IsNullOrEmpty(options.ServiceInstanceId))
            {
                logger.LogInformation(
                    "Quilt4Net startup: {ApplicationName} [{ServiceInstanceId}] v{Version} in {Environment}",
                    options.ApplicationName ?? "(unknown)",
                    options.ServiceInstanceId,
                    options.Version ?? "(unknown)",
                    options.Environment ?? "(unknown)");
            }
            else
            {
                logger.LogInformation(
                    "Quilt4Net startup: {ApplicationName} v{Version} in {Environment}",
                    options.ApplicationName ?? "(unknown)",
                    options.Version ?? "(unknown)",
                    options.Environment ?? "(unknown)");
            }
        }
    }
}
