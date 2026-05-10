using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

internal static class ApplicationInsightsErrorMessage
{
    public static string Format(string prefix, Exception ex)
        => Format(prefix, ex, null, null);

    public static string Format(string prefix, Exception ex, string incidentId)
        => Format(prefix, ex, incidentId, null);

    /// <summary>
    /// Auth-failure messages now include the failing workspace's id when the calling
    /// component knows it — so the user can identify which AI configuration to fix
    /// instead of bisecting through the configuration list themselves.
    /// </summary>
    public static string Format(string prefix, Exception ex, string incidentId, IApplicationInsightsContext context)
    {
        var body = IsAuthenticationFailure(ex)
            ? FormatAuthFailure(context)
            : ex.Message;

        return string.IsNullOrEmpty(incidentId)
            ? $"{prefix} {body}"
            : $"{prefix} {body} [Incident: {incidentId}]";
    }

    private static string FormatAuthFailure(IApplicationInsightsContext context)
    {
        var workspace = context?.WorkspaceId;
        var which = string.IsNullOrEmpty(workspace) ? string.Empty : $" for workspace {workspace}";
        return $"Application Insights authentication failed{which} — the configured TenantId, ClientId or ClientSecret may be incorrect, the secret may have expired, or the service principal may not have access to the workspace.";
    }

    /// <summary>
    /// Razor catch sites use this to decide whether to render a "fix the configuration" link
    /// next to the error alert — the configuration page is only useful when this specific
    /// failure mode is the cause.
    /// </summary>
    internal static bool IsAuthenticationFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.GetType().FullName == "Azure.Identity.AuthenticationFailedException") return true;
            if (current.Message?.Contains("AADSTS", StringComparison.Ordinal) == true) return true;
            if (current.Message?.Contains("ClientSecretCredential", StringComparison.Ordinal) == true) return true;
        }
        return false;
    }
}
