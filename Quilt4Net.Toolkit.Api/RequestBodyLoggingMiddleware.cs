using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Measure;
using Quilt4Net.Toolkit.Framework;

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

        var sw = Stopwatch.StartNew();
        try
        {
            using var responseBodyStream = new MemoryStream();
            // Replace the response body with a memory stream
            context.Response.Body = responseBodyStream;

            // Call the next middleware
            await _next(context);

            sw.Stop();

            // Read and capture the response details
            var responseDetails = await CaptureResponseDetailsAsync(context);

            // Log both request and response in a single log entry
            LogRequestAndResponse(requestDetails, responseDetails, sw.Elapsed);

            // Copy the response back to the original stream
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception e)
        {
            LogRequestAndResponse(requestDetails, null, sw.Elapsed, e);
            throw;
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
        var query = BuildQueries(context.Request.Query);

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
            Headers = headers.ToUniqueDictionary(),
            Query = query.ToUniqueDictionary(),
            Body = body,
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

    private static IEnumerable<KeyValuePair<string, string>> BuildQueries(IQueryCollection queries)
    {
        foreach (var querie in queries)
        {
            yield return new KeyValuePair<string, string>(querie.Key, querie.Value);
        }
    }

    private async Task<Response> CaptureResponseDetailsAsync(HttpContext context)
    {
        var headers = BuildHeaders(context.Response.Headers);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin); // Reset stream position

        return new Response
        {
            StatusCode = context.Response.StatusCode,
            Headers = headers.ToUniqueDictionary(),
            Body = body,
        };
    }

    private void LogRequestAndResponse(Request request, Response response, TimeSpan elapsed, Exception e = default)
    {
        var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
        var responseJson = response != null ? System.Text.Json.JsonSerializer.Serialize(response) : null;
        var details = BuildDetails(e);

        if (e == null)
        {
            _logger.LogInformation("Http {Method} to {Path} in {Elapsed} ms. {Request} {Response} {Details}", request.Method, request.Path, elapsed, requestJson, responseJson, details);
        }
        else
        {
            _logger.LogError("Http {Method} to {Path} in {Elapsed} ms, failed {ErrorMessage} @{StackTrace}. {Request} {Details}", request.Method, request.Path, elapsed, e.Message, e.StackTrace, requestJson, details);
        }
    }

    private string BuildDetails(Exception e)
    {
        var dictionary = new Dictionary<string, string>
        {
            { "Monitor", Constants.Monitor },
            { "Method", "Http" }
        };
        if (e != null)
        {
            foreach (var data in e.GetData())
            {
                dictionary.TryAdd(data.Key, $"{data.Value}");
            }
        }

        var d = dictionary.Where(x => !string.IsNullOrEmpty(x.Value)).ToUniqueDictionary();
        var details = System.Text.Json.JsonSerializer.Serialize(d);
        return details;
    }

    internal record Request
    {
        public required string Method { get; init; }
        public required string Path { get; init; }
        public required Dictionary<string, string> Headers { get; init; }
        public required Dictionary<string, string> Query { get; init; }
        public required string Body { get; init; }
        public required string ClientIp { get; init; }
    }

    internal record Response
    {
        public required int StatusCode { get; init; }
        public required Dictionary<string, string> Headers { get; init; }
        public required string Body { get; init; }
    }
}