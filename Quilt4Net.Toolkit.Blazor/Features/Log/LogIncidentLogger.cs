using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Diagnostics;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

internal static class LogIncidentLogger
{
    /// <summary>
    /// Mints an incident id, logs the exception with structured properties,
    /// and returns a user-facing message that includes the same id so the
    /// reporter and the log entry can be cross-referenced.
    /// </summary>
    public static string LogAndFormat(
        ILogger logger,
        Exception ex,
        string componentName,
        string userPrefix)
    {
        var incidentId = IncidentId.New();
        logger.LogError(ex,
            "AI call failed. Incident={IncidentId} Component={Component}",
            incidentId, componentName);
        return ApplicationInsightsErrorMessage.Format(userPrefix, ex, incidentId);
    }
}
