using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    /// <summary>
    /// Register client for reading data from the health API.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetHealthClient(this IServiceCollection services, Action<HealthClientOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        var config = configuration?.GetSection("Quilt4Net:HealthClient").Get<HealthClientOptions>();
        var o = new HealthClientOptions
        {
            HealthAddress = config?.HealthAddress.NullIfEmpty() ?? throw new InvalidOperationException($"No address for {nameof(HealthClient)} has been configured.")
        };

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddTransient<IHealthClient, HealthClient>();
    }
}