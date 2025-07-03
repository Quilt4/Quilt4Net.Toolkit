﻿using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc.Controllers;
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
        var endpoint = context.GetEndpoint(); ;
        var loggingAttr = endpoint?.Metadata.GetMetadata<LoggingAttribute>();
        loggingAttr ??= endpoint?.Metadata.GetMetadata<LoggingStreamAttribute>() == null ? null : new LoggingAttribute { ResponseBody = false };

        //NOTE: Behaviour when logging attribute has not been set.
        var logResponseBody = true;
        if (loggingAttr == null)
        {
            //TODO: Make it possible to configure a pattern, perhaps with regex in Quilt4NetApiOptions.
            if (!context.Request.Path.StartsWithSegments("/Api"))
            {
                await _next(context);
                return;
            }

            //var methodInfo = endpoint?.Metadata.GetMetadata<MethodInfo>();
            var methodInfo = (endpoint as RouteEndpoint)?.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault()?.MethodInfo;
            logResponseBody = methodInfo?.ReturnType != typeof(Task);
        }

        //NOTE: Do not log when logging has been manually disabled:
        var enableLogging = loggingAttr?.Enabled ?? true;
        if (!enableLogging)
        {
            await _next(context);
            return;
        }

        // Use flags to control what is logged
        var logRequestBody = loggingAttr?.RequestBody ?? true;
        logResponseBody = loggingAttr?.ResponseBody ?? logResponseBody;

        context.Request.EnableBuffering();
        var requestDetails = await CaptureRequestDetailsAsync(context, logRequestBody, _options.Logging?.MaxBodySize ?? Constants.MaxBodySize);
        var originalResponseBodyStream = context.Response.Body;
        var correlationId = context.Items.TryGetValue("CorrelationId", out var c) ? c?.ToString() : null;

        var sw = new Stopwatch();
        var telemetry = GetRequestTelemetry(context);

        try
        {
            using var responseBodyStream = new MemoryStream();
            if (logResponseBody)
            {
                context.Response.Body = responseBodyStream;
            }

            if (telemetry != null)
            {
                telemetry.Properties["UserId"] = context.User.Identity?.Name ?? "Anonymous";
                telemetry.Properties["Request"] = System.Text.Json.JsonSerializer.Serialize(requestDetails);
                if (!string.IsNullOrEmpty(correlationId)) telemetry.Properties["CorrelationId"] = correlationId;
                var asm = Assembly.GetEntryAssembly();
                var nm = asm?.GetName();
                if (nm != null)
                {
                    telemetry.Properties["ApplicationName"] = nm.Name;
                    telemetry.Properties["Version"] = $"{nm.Version}";
                }
            }

            sw.Start();
            await _next(context);
            sw.Stop();

            var responseDetails = await CaptureResponseDetailsAsync(context, logResponseBody, _options.Logging?.MaxBodySize ?? Constants.MaxBodySize);
            if (telemetry != null)
            {
                telemetry.Properties["Response"] = System.Text.Json.JsonSerializer.Serialize(responseDetails);
                telemetry.Properties["Elapsed"] = $"{sw.Elapsed}";
                telemetry.Properties["Details"] = BuildDetails(default);
            }

            LogRequestAndResponse(requestDetails, responseDetails, sw.Elapsed, correlationId);

            if (logResponseBody)
            {
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
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
        if (_options.Logging?.LogHttpRequest.HasFlag(HttpRequestLogMode.ApplicationInsights) ?? false)
        {
            telemetry = context.Features.Get<RequestTelemetry>();
        }

        return telemetry;
    }

    private async Task<Request> CaptureRequestDetailsAsync(HttpContext context, bool logBody, long maxBodySize)
    {
        if (maxBodySize == 0) logBody = false;

        var headers = BuildHeaders(context.Request.Headers);
        var query = BuildQueries(context.Request.Query);

        var body = "[Not logged]";
        if (logBody)
        {
            context.Request.EnableBuffering();

            if (context.Request.ContentLength.HasValue && context.Request.ContentLength <= maxBodySize)
            {
                body = await ReadRequestBodyAsync(context.Request);
            }
            else if (!context.Request.ContentLength.HasValue)
            {
                // Read into buffer and cut off if too big
                using var memStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(memStream);
                if (memStream.Length <= maxBodySize)
                {
                    context.Request.Body.Position = 0;
                    body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
                }
                else
                {
                    body = $"[Skipped: Request body exceeds {maxBodySize} bytes]";
                }
            }
            else
            {
                body = $"[Skipped: Request body exceeds {maxBodySize} bytes]";
            }

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

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
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

    private async Task<Response> CaptureResponseDetailsAsync(HttpContext context, bool logBody, long maxBodySize)
    {
        if (maxBodySize == 0) logBody = false;

        var headers = BuildHeaders(context.Response.Headers);
        var body = "[Not logged]";

        if (logBody)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            var buffer = new char[maxBodySize + 1];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

            if (read > maxBodySize)
            {
                body = $"[Skipped: Response body exceeds {maxBodySize} bytes]";
            }
            else
            {
                body = new string(buffer, 0, read);
            }

            context.Response.Body.Seek(0, SeekOrigin.Begin);
        }

        return new Response
        {
            StatusCode = context.Response.StatusCode,
            Headers = headers.ToUniqueDictionary(),
            Body = body,
        };
    }

    private void LogRequestAndResponse(Request request, Response response, TimeSpan elapsed, string correlationId, Exception e = default)
    {
        if (!(_options.Logging?.LogHttpRequest.HasFlag(HttpRequestLogMode.Logger) ?? false)) return;

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
            { "Monitor", _options.Logging?.MonitorName ?? Constants.Monitor },
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