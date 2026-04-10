using System.Reflection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Features.Logging;

namespace Quilt4Net.Toolkit;

public static class LoggingRegistration
{
    /// <summary>
    /// Register universal telemetry tagging for all Application Insights telemetry.
    /// </summary>
    public static Quilt4NetLoggingBuilder AddQuilt4NetLogging(this IHostApplicationBuilder builder, Action<Quilt4NetLoggingOptions> options = null)
    {
        return builder.Services.AddQuilt4NetLogging(builder.Configuration, options, builder.Environment?.EnvironmentName);
    }

    /// <summary>
    /// Register universal telemetry tagging for all Application Insights telemetry.
    /// </summary>
    public static Quilt4NetLoggingBuilder AddQuilt4NetLogging(this IServiceCollection services, IConfiguration configuration = null, Action<Quilt4NetLoggingOptions> options = null, string environmentName = null)
    {
        var config = configuration?.GetSection("Quilt4Net:Logging").Get<Quilt4NetLoggingOptions>();

        var entryAssembly = Assembly.GetEntryAssembly();
        var o = new Quilt4NetLoggingOptions
        {
            ApplicationName = config?.ApplicationName ?? entryAssembly?.GetName().Name,
            Version = config?.Version ?? entryAssembly?.GetName().Version?.ToString(),
            Environment = config?.Environment
                          ?? environmentName
                          ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                          ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Production"
        };

        options?.Invoke(o);

        services.AddSingleton(o);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, Quilt4NetTelemetryInitializer>());

        return new Quilt4NetLoggingBuilder(services, o);
    }
}
