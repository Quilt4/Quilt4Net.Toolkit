using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Logging;

internal static class Quilt4NetStartupLogger
{
    public static void Log(ILogger logger, Quilt4NetLoggingOptions options)
    {
        using (logger.BeginScope(new Dictionary<string, object> { ["Quilt4NetStartup"] = "true" }))
        {
            logger.LogInformation(
                "Quilt4Net startup: {ApplicationName} v{Version} in {Environment}",
                options.ApplicationName ?? "(unknown)",
                options.Version ?? "(unknown)",
                options.Environment ?? "(unknown)");
        }
    }
}
