using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Api;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable buffering for the request body
        context.Request.EnableBuffering();

        // Read and capture the request details
        var requestDetails = await CaptureRequestDetailsAsync(context);

        // Intercept the response
        var originalResponseBodyStream = context.Response.Body;

        try
        {
            using (var responseBodyStream = new MemoryStream())
            {
                // Replace the response body with a memory stream
                context.Response.Body = responseBodyStream;

                // Call the next middleware
                await _next(context);

                // Read and capture the response details
                var responseDetails = await CaptureResponseDetailsAsync(context);

                // Log both request and response in a single log entry
                LogRequestAndResponse(requestDetails, responseDetails);

                // Copy the response back to the original stream
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
        }
        finally
        {
            // Restore the original response body stream
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private async Task<Request> CaptureRequestDetailsAsync(HttpContext context)
    {
        var headers = BuildHeaders(context.Request.Headers);

        string body;
        using (var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        return new Request
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            Headers = headers.ToDictionary(x => x.Key, x => x.Value),
            Body = System.Text.Json.JsonSerializer.Deserialize<dynamic>(body),
            ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildHeaders(IHeaderDictionary headers)
    {
        foreach (var header in headers)
        {
            yield return new KeyValuePair<string, string>(header.Key, header.Value);
        }
    }

    private async Task<Response> CaptureResponseDetailsAsync(HttpContext context)
    {
        // Capture response headers
        var headers = new StringBuilder();
        foreach (var header in context.Response.Headers)
        {
            headers.AppendLine($"{header.Key}: {header.Value}");
        }

        // Read response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin); // Reset stream position

        return new Response { };
        //return $"Response {context.Response.StatusCode}\nHeaders:\n{headers}\nBody:\n{body}";
    }

    private void LogRequestAndResponse(Request request, Response response)
    {
        _logger.LogInformation("HTTP Transaction:\n{request}\n{response}", request, response);
    }

    internal record Request
    {
        public required string Method { get; init; }
        public required string Path { get; init; }
        public required Dictionary<string, string> Headers { get; init; }
        public required dynamic Body { get; init; }
        public required string ClientIp { get; init; }
    }

    internal record Response
    {

    }
}