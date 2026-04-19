using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace Quilt4Net.Toolkit;

public static class LoggingRegistration
{
    /// <summary>
    /// Register universal telemetry identity (service.name, service.version, deployment.environment, service.instance.id)
    /// on the OpenTelemetry Resource. Applies to traces, logs and metrics.
    /// </summary>
    public static Quilt4NetLoggingBuilder AddQuilt4NetLogging(this IHostApplicationBuilder builder, Action<Quilt4NetLoggingOptions> options = null)
    {
        return builder.Services.AddQuilt4NetLogging(builder.Configuration, options, builder.Environment?.EnvironmentName, builder.Environment?.ApplicationName);
    }

    /// <summary>
    /// Register universal telemetry identity (service.name, service.version, deployment.environment, service.instance.id)
    /// on the OpenTelemetry Resource. Applies to traces, logs and metrics.
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

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                if (!string.IsNullOrEmpty(o.ApplicationName))
                {
                    resource.AddService(
                        serviceName: o.ApplicationName,
                        serviceVersion: string.IsNullOrEmpty(o.Version) ? null : o.Version,
                        serviceInstanceId: System.Environment.MachineName);
                }

                if (!string.IsNullOrEmpty(o.Environment))
                {
                    resource.AddAttributes(new KeyValuePair<string, object>[]
                    {
                        new("deployment.environment", o.Environment),
                    });
                }
            });

        return new Quilt4NetLoggingBuilder(services, o);
    }

    private static string ResolveApplicationNameFromEntryAssembly()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var name = entryAssembly?.GetName().Name;

        if (string.IsNullOrEmpty(name)) return null;

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
