namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Shared correlation-id constants used by both the inbound server middleware
/// (<c>Quilt4Net.Toolkit.Api.Framework.CorrelationIdMiddleware</c>) and the outbound Toolkit HTTP
/// clients, so the header name and the <see cref="HttpContext"/> item key never drift apart.
/// </summary>
public static class CorrelationConstants
{
    /// <summary>HTTP header carrying the correlation id across service hops.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Key under which the server middleware stores the resolved id on <c>HttpContext.Items</c>.</summary>
    public const string ItemKey = "CorrelationId";
}
