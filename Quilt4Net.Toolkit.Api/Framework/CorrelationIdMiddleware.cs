using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Api.Framework;

public class CorrelationIdMiddleware
{
    private const string HeaderName = CorrelationConstants.HeaderName;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            headerValue = Guid.NewGuid().ToString();
        }

        var correlationId = headerValue.ToString();
        context.Response.Headers[HeaderName] = correlationId;
        context.Items[CorrelationConstants.ItemKey] = correlationId;

        // Push CorrelationId into a logging scope so every ILogger call made during this
        // request inherits it as a structured property. Scope values only reach
        // customDimensions when scope capture is enabled, so this id lands on every
        // AppTrace / AppException for the request when the app opts in via
        // AddQuilt4NetLogging(o => o.IncludeScopes = true) — letting the same correlation id
        // span the inbound request, any work it triggers, and the outgoing response.
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
