using Microsoft.AspNetCore.Http;

namespace Quilt4Net.Toolkit.Api;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for an existing correlation ID
        if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            // Generate a new correlation ID if not provided
            correlationId = Guid.NewGuid().ToString();
        }

        // Add the correlation ID to the response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        // Store it for logging or other purposes
        context.Items["CorrelationId"] = correlationId;

        await _next(context);
    }
}