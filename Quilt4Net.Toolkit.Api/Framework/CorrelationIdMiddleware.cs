using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Api.Framework;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";

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
        context.Items["CorrelationId"] = correlationId;

        // Push CorrelationId into a logging scope so every ILogger call made during this
        // request inherits it as a structured property. The Azure Monitor exporter then
        // writes customDimensions["CorrelationId"] on every AppTrace / AppException for
        // the duration of the request — letting the same correlation id span the inbound
        // request, any work it triggers, and the outgoing response.
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
