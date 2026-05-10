using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.Diagnostics;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

internal static class LogIncidentLogger
{
    /// <summary>
    /// Mints an incident id, logs the exception with structured properties (including the
    /// failing workspace when known), and returns a user-facing message that includes the
    /// same id so the reporter and the log entry can be cross-referenced. The optional
    /// <paramref name="context"/> lets the message identify which AI configuration is
    /// broken when the failure is an authentication failure.
    /// </summary>
    public static string LogAndFormat(
        ILogger logger,
        Exception ex,
        string componentName,
        string userPrefix,
        IApplicationInsightsContext context = null)
    {
        var incidentId = IncidentId.New();
        logger.LogError(ex,
            "AI call failed. Incident={IncidentId} Component={Component} Workspace={Workspace}",
            incidentId, componentName, context?.WorkspaceId);
        return ApplicationInsightsErrorMessage.Format(userPrefix, ex, incidentId, context);
    }
}
