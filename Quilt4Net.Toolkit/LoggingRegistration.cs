using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quilt4Net.Toolkit.Features.Logging;

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
    /// Also registers <see cref="Quilt4NetStartupHostedService"/> so apps with an <see cref="IHost"/>
    /// (including those built via <see cref="IHostApplicationBuilder"/>, the older <c>IHostBuilder</c>
    /// pattern, or <c>Tharga.Wpf</c>'s generic-host setup) emit the startup entry automatically.
    /// Truly no-host contexts should call <see cref="LogQuilt4NetStartup"/> manually.
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

        // Resolve service.instance.id AFTER the option lambda so the user's explicit value
        // wins, then fall back to the OTel-standard env var, then the Quilt4Net shorthand.
        // Storing the resolved value back onto the options so the startup-line emitter
        // reads the same value the resource gets.
        o.ServiceInstanceId = Features.Logging.ServiceInstanceIdResolver.Resolve(o.ServiceInstanceId);

        services.AddSingleton(o);

        services.AddHostedService<Quilt4NetStartupHostedService>();

        var identity = new Features.Logging.TelemetryIdentity(
            Environment: o.Environment,
            ApplicationName: o.ApplicationName,
            Version: o.Version,
            MachineName: System.Environment.MachineName,
            MonitorName: o.MonitorName,
            ServiceInstanceId: o.ServiceInstanceId);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                if (!string.IsNullOrEmpty(o.ApplicationName))
                {
                    // serviceInstanceId: when the user / env supplied a value, surface it on the
                    // resource (so cloud_RoleInstance carries the variant); otherwise keep the
                    // historical MachineName fallback so existing consumers see no change.
                    resource.AddService(
                        serviceName: o.ApplicationName,
                        serviceVersion: string.IsNullOrEmpty(o.Version) ? null : o.Version,
                        serviceInstanceId: string.IsNullOrEmpty(o.ServiceInstanceId)
                            ? System.Environment.MachineName
                            : o.ServiceInstanceId);
                }

                if (!string.IsNullOrEmpty(o.Environment))
                {
                    resource.AddAttributes(new KeyValuePair<string, object>[]
                    {
                        new("deployment.environment", o.Environment),
                    });
                }
            })
            // Resource attributes alone don't reach per-row Properties via the Azure Monitor
            // exporter (only the well-known service.* mappings → cloud_RoleName / AppVersion /
            // cloud_RoleInstance columns). The processors below copy the identity onto each
            // log record and span as per-record attributes so they land in customDimensions.
            .WithLogging(b => b.AddProcessor(new Features.Logging.TelemetryIdentityLogProcessor(identity)))
            .WithTracing(b => b.AddProcessor(new Features.Logging.TelemetryIdentityActivityProcessor(identity)));

        return new Quilt4NetLoggingBuilder(services, o);
    }

    /// <summary>
    /// Manually emit the Quilt4Net startup log entry. Use this **only** when the calling code
    /// has no <see cref="IHost"/> and therefore no <see cref="IHostedService"/> runs — e.g. a
    /// pure script, a static-<c>Main</c> console without `Microsoft.Extensions.Hosting`, or a
    /// plain `Application.Run` WPF app that doesn't build a host.
    ///
    /// Apps that DO build an <see cref="IHost"/> — including those wired via
    /// <see cref="IHostApplicationBuilder"/>, the older <c>IHostBuilder</c> pattern, or
    /// <c>Tharga.Wpf</c>'s generic-host setup — get the startup entry automatically via
    /// <see cref="Quilt4NetStartupHostedService"/> regardless of which <c>AddQuilt4NetLogging</c>
    /// overload was used to register it (the <see cref="IServiceCollection"/> overload registers
    /// the hosted service the same as the <see cref="IHostApplicationBuilder"/> overload).
    /// Calling this method in those contexts produces a duplicate startup entry.
    /// </summary>
    public static void LogQuilt4NetStartup(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<Quilt4NetLoggingOptions>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Quilt4NetStartupHostedService>();
        Quilt4NetStartupLogger.Log(logger, options);
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
