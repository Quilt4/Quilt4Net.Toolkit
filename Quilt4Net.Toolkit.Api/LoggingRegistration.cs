using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Framework;

namespace Quilt4Net.Toolkit.Api;

public static class LoggingRegistration
{
    private static LoggingOptions _options;

    [Obsolete($"Use {nameof(AddQuilt4NetApiLogging)} instead.")]
    public static void AddQuilt4NetLogging(this WebApplicationBuilder builder, Action<LoggingOptions> options = null)
    {
        AddQuilt4NetLogging(builder.Services, options);
    }

    [Obsolete($"Use {nameof(AddQuilt4NetApiLogging)} instead.")]
    public static void AddQuilt4NetLogging(this IServiceCollection services, Action<LoggingOptions> options = null)
    {
        services.AddQuilt4NetApiLogging(options);
    }

    /// <summary>
    /// Add logging for API calls.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetApiLogging(this IServiceCollection services, Action<LoggingOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        _options = configuration?.GetSection("Quilt4Net:ApiLogging").Get<LoggingOptions>()
                   ?? configuration?.GetSection("Quilt4Net:Logging").Get<LoggingOptions>()
                   ?? new LoggingOptions();

        options?.Invoke(_options);
        services.AddSingleton(Options.Create(_options));

        services.AddSingleton(_ => new CompiledLoggingOptions(_options));
    }

    public static void UseQuilt4NetLogging(this WebApplication app)
    {
        if (_options == null) throw new InvalidOperationException($"Call {nameof(AddQuilt4NetLogging)} before {nameof(UseQuilt4NetLogging)}.");

        if (_options?.UseCorrelationId ?? false)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        RegisterLoggingMiddleware(app);
    }

    private static void RegisterLoggingMiddleware(WebApplication app)
    {
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