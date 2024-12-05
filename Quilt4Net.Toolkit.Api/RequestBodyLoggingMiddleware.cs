using System.Text;
using Microsoft.AspNetCore.Http;

namespace Quilt4Net.Toolkit.Api;

public class RequestBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestBodyLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Intercept Query Parameters
        var queryParameters = context.Request.Query;
        foreach (var param in queryParameters)
        {
            Console.WriteLine($"Query Parameter: {param.Key} = {param.Value}");
        }

        // Intercept Headers
        var headers = context.Request.Headers;
        foreach (var header in headers)
        {
            Console.WriteLine($"Header: {header.Key} = {header.Value}");
        }

        // Enable buffering to read the request body
        context.Request.EnableBuffering();

        // Read the request body
        using (var reader = new StreamReader(
                   context.Request.Body,
                   encoding: Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   bufferSize: 1024,
                   leaveOpen: true))
        {
            var requestBody = await reader.ReadToEndAsync();
            Console.WriteLine($"Request Body: {requestBody}");

            // Reset the request body stream position for the next middleware
            context.Request.Body.Position = 0;
        }

        // Pass control to the next middleware
        await _next(context);
    }
}