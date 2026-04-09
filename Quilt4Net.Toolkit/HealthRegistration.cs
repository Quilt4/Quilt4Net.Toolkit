using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    /// <summary>
    /// Register client for reading data from the health API.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetHealthClient(this IHostApplicationBuilder builder, Action<HealthClientOptions> options = null)
    {
        builder.Services.AddQuilt4NetHealthClient(builder.Configuration, options);
    }

    /// <summary>
    /// Register client for reading data from the health API.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetHealthClient(this IServiceCollection services, IConfiguration configuration, Action<HealthClientOptions> options = null)
    {
        var config = configuration?.GetSection("Quilt4Net:HealthClient").Get<HealthClientOptions>();
        var o = new HealthClientOptions
        {
            HealthAddress = config?.HealthAddress.NullIfEmpty()
        };

        options?.Invoke(o);

        if (string.IsNullOrEmpty(o.HealthAddress))
        {
            throw new InvalidOperationException($"No address for {nameof(HealthClient)} has been configured.");
        }

        services.AddSingleton(Options.Create(o));

        services.AddTransient<IHealthClient, HealthClient>();
    }
}