using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Measure;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Api;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Quilt4NetApiOptions _options;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, Quilt4NetApiOptions options, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestDetails = await CaptureRequestDetailsAsync(context);
        var originalResponseBodyStream = context.Response.Body;
        var correlationId = context.Items.TryGetValue("CorrelationId", out var c) ? c?.ToString() : null;

        var sw = new Stopwatch();
        var telemetry = GetRequestTelemetry(context);

        try
        {
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            if (telemetry != null)
            {
                telemetry.Properties["UserId"] = context.User.Identity?.Name ?? "Anonymous";
                telemetry.Properties["Request"] = System.Text.Json.JsonSerializer.Serialize(requestDetails);
                if (string.IsNullOrEmpty(correlationId)) telemetry.Properties["CorrelationId"] = correlationId;
            }

            sw.Start();
            await _next(context);
            sw.Stop();

            var responseDetails = await CaptureResponseDetailsAsync(context);
            if (telemetry != null)
            {
                telemetry.Properties["Response"] = System.Text.Json.JsonSerializer.Serialize(responseDetails);
                telemetry.Properties["Elapsed"] = $"{sw.Elapsed}";
                telemetry.Properties["Details"] = BuildDetails(default);
            }

            LogRequestAndResponse(requestDetails, responseDetails, sw.Elapsed, correlationId);

            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception e)
        {
            if (telemetry != null)
            {
                telemetry.Properties["StackTrace"] = e.StackTrace;
                telemetry.Properties["Elapsed"] = $"{sw.Elapsed}";
                telemetry.Properties["Details"] = BuildDetails(e);
            }

            LogRequestAndResponse(requestDetails, null, sw.Elapsed, correlationId, e);
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private RequestTelemetry GetRequestTelemetry(HttpContext context)
    {
        RequestTelemetry telemetry = null;
        if (_options.LogHttpRequest.HasFlag(HttpRequestLogMode.ApplicationInsights))
        {
            telemetry = context.Features.Get<RequestTelemetry>();
        }

        return telemetry;
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

    private void LogRequestAndResponse(Request request, Response response, TimeSpan elapsed, string correlationId, Exception e = default)
    {
        if (!_options.LogHttpRequest.HasFlag(HttpRequestLogMode.Logger)) return;

        var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
        var responseJson = response != null ? System.Text.Json.JsonSerializer.Serialize(response) : null;
        var details = BuildDetails(e);

        if (e == null)
        {
            _logger.LogInformation("Http {Method} to {Path} in {Elapsed} ms. {Request} {Response} {Details} CorrelationId: {CorrelationId}", request.Method, request.Path, elapsed, requestJson, responseJson, details, correlationId);
        }
        else
        {
            _logger.LogError("Http {Method} to {Path} in {Elapsed} ms, failed {ErrorMessage} @{StackTrace}. {Request} {Details} CorrelationId: {CorrelationId}", request.Method, request.Path, elapsed, e.Message, e.StackTrace, requestJson, details, correlationId);
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