namespace Quilt4Net.Toolkit.Blazor.Features.Log;

internal static class ApplicationInsightsErrorMessage
{
    public static string Format(string prefix, Exception ex)
        => Format(prefix, ex, null);

    public static string Format(string prefix, Exception ex, string incidentId)
    {
        var body = IsAuthenticationFailure(ex)
            ? "Application Insights authentication failed — the configured TenantId, WorkspaceId, ClientId or ClientSecret may be incorrect, the secret may have expired, or the service principal may not have access to the workspace. Verify the Application Insights settings."
            : ex.Message;

        return string.IsNullOrEmpty(incidentId)
            ? $"{prefix} {body}"
            : $"{prefix} {body} [Incident: {incidentId}]";
    }

    private static bool IsAuthenticationFailure(Exception ex)
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
