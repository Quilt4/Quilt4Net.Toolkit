using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Configuration for logging.
/// </summary>
public record LoggingOptions
{
    /// <summary>
    /// Add logger for Http request and response with body, headers, query and results.
    /// Default is append to Application Insights requests.
    /// Remember to also add 'builder.Logging.AddApplicationInsights();' at startup and add connection string, if you are using ApplicationInsights.
    /// </summary>
    public HttpRequestLogMode LogHttpRequest { get; set; } = HttpRequestLogMode.ApplicationInsights;

    /// <summary>
    /// If this is added calls to the API picks up 'X-Correlation-ID' from the header and append that to logging on the server.
    /// If there is no CorrelationId provided, one is added and returned with the response to the client.
    /// If scoped, the CorrelationId can be added by...
    /// On the client side use ...
    /// </summary>
    public bool UseCorrelationId { get; set; } = true;

    /// <summary>
    /// Monitor name used to track log-items to selected monitor.
    /// If set to empty string the value will be omitted.
    /// Default is Quilt4Net.
    /// </summary>
    public string MonitorName { get; set; } = Constants.Monitor;

    /// <summary>
    /// The maximum size of the body (request and response) to be logged.
    /// Default is 1 MB.
    /// If set to 0, the body is not logged.
    /// </summary>
    public long MaxBodySize { get; set; } = Constants.MaxBodySize;

    /// <summary>
    /// List of paths to include for logging.
    /// Default is to include all paths that starts with "/Api".
    /// All values are case-insensitive.
    /// To include all paths provide the value ".*".
    /// </summary>
    public string[] IncludePaths { get; set; } = ["^/Api"];

    /// <summary>
    /// Create interceptor for the logger so that information can be modified.
    /// This can be used to remove secrets from logging.
    /// </summary>
    public Func<Request, Response, Dictionary<string, string>, IServiceProvider, Task<(Request, Response, Dictionary<string, string>)>> Interceptor;
}