using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Framework;

namespace Quilt4Net.Toolkit.Api;

public static class HttpRequestLoggingRegistration
{
    /// <summary>
    /// Add HTTP request/response logging to the Quilt4Net logging pipeline.
    /// Registers correlation ID and request/response body logging middleware.
    /// </summary>
    public static Quilt4NetLoggingBuilder AddHttpRequestLogging(this Quilt4NetLoggingBuilder builder, Action<LoggingOptions> options = null)
    {
        var loggingOptions = new LoggingOptions();
        options?.Invoke(loggingOptions);

        builder.Services.AddSingleton(Options.Create(loggingOptions));
        builder.Services.AddSingleton(_ => new CompiledLoggingOptions(loggingOptions));

        return builder;
    }

    /// <summary>
    /// Activate Quilt4Net logging middleware (correlation ID, request/response logging).
    /// Only activates HTTP middleware if AddHttpRequestLogging was called during registration.
    /// </summary>
    public static WebApplication UseQuilt4NetLogging(this WebApplication app)
    {
        var loggingOptions = app.Services.GetService<IOptions<LoggingOptions>>()?.Value;

        if (loggingOptions == null) return app;

        if (loggingOptions.UseCorrelationId)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        if (loggingOptions.LogHttpRequest > HttpRequestLogMode.None)
        {
            app.UseWhen(
                _ => true,
                branch => branch.UseMiddleware<RequestResponseLoggingMiddleware>()
            );
        }

        return app;
    }
}
