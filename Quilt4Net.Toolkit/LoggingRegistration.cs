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
        return builder.Services.AddQuilt4NetLogging(builder.Configuration, options, builder.Environment?.EnvironmentName, builder.Environment?.ApplicationName);
    }

    /// <summary>
    /// Register universal telemetry tagging for all Application Insights telemetry.
    /// </summary>
    public static Quilt4NetLoggingBuilder AddQuilt4NetLogging(this IServiceCollection services, IConfiguration configuration = null, Action<Quilt4NetLoggingOptions> options = null, string environmentName = null, string applicationName = null)
    {
        var config = configuration?.GetSection("Quilt4Net:Logging").Get<Quilt4NetLoggingOptions>();

        var resolvedName = applicationName ?? config?.ApplicationName ?? ResolveApplicationNameFromEntryAssembly();
        var resolvedVersion = config?.Version ?? ResolveVersion(resolvedName);
        var o = new Quilt4NetLoggingOptions
        {
            ApplicationName = resolvedName,
            Version = resolvedVersion,
            Environment = environmentName
                          ?? config?.Environment
                          ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                          ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Production"
        };

        options?.Invoke(o);

        services.AddSingleton(o);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, Quilt4NetTelemetryInitializer>());

        return new Quilt4NetLoggingBuilder(services, o);
    }

    private static string ResolveApplicationNameFromEntryAssembly()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var name = entryAssembly?.GetName().Name;

        if (string.IsNullOrEmpty(name)) return null;

        // Avoid defaulting to a framework assembly (Blazor WASM, in-process IIS, test runners can return these)
        if (name.StartsWith("Microsoft.", StringComparison.Ordinal)
            || name.StartsWith("System.", StringComparison.Ordinal)
            || name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return name;
    }

    private static string ResolveVersion(string applicationName)
    {
        // Try the named application assembly first (set by the host via IHostEnvironment.ApplicationName)
        if (!string.IsNullOrEmpty(applicationName))
        {
            try
            {
                var assembly = Assembly.Load(applicationName);
                var version = assembly.GetName().Version?.ToString();
                if (!string.IsNullOrEmpty(version)) return version;
            }
            catch
            {
                // Fall through to entry assembly
            }
        }

        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}
