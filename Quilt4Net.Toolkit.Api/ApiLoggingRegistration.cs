using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Framework;

namespace Quilt4Net.Toolkit.Api;

public static class ApiLoggingRegistration
{
    private static LoggingOptions _options;

    [Obsolete("Use builder.AddQuilt4NetLogging().AddHttpRequestLogging() instead.")]
    public static void AddQuilt4NetApiLogging(this IHostApplicationBuilder builder, Action<LoggingOptions> options = null)
    {
        builder.Services.AddQuilt4NetApiLogging(builder.Configuration, options);
    }

    [Obsolete("Use builder.AddQuilt4NetLogging().AddHttpRequestLogging() instead.")]
    public static void AddQuilt4NetApiLogging(this IServiceCollection services, IConfiguration configuration, Action<LoggingOptions> options = null)
    {
        _options = configuration?.GetSection("Quilt4Net:ApiLogging").Get<LoggingOptions>()
                   ?? configuration?.GetSection("Quilt4Net:Logging").Get<LoggingOptions>()
                   ?? new LoggingOptions();

        options?.Invoke(_options);
        services.AddSingleton(Options.Create(_options));

        services.AddSingleton(_ => new CompiledLoggingOptions(_options));
    }

    [Obsolete("Use app.UseQuilt4NetLogging() instead.")]
    public static void UseQuilt4NetApiLogging(this WebApplication app)
    {
        if (_options == null) throw new InvalidOperationException($"Call {nameof(AddQuilt4NetApiLogging)} before {nameof(UseQuilt4NetApiLogging)}.");

        if (_options?.UseCorrelationId ?? false)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        if ((_options?.LogHttpRequest ?? HttpRequestLogMode.None) > HttpRequestLogMode.None)
        {
            app.UseWhen(
                _ => true,
                branch =>
                {
                    branch.UseMiddleware<RequestResponseLoggingMiddleware>();
                }
            );
        }
    }
}
