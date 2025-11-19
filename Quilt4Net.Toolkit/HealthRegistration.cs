using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    public static void AddQuilt4NetHealthClient(this IServiceCollection services, Action<HealthOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        var address = configuration?.GetSection("Quilt4Net").GetSection("HealthAddress").Value;

        var config = configuration?.GetSection("Quilt4Net:Health").Get<HealthOptions>();
        var o = new HealthOptions
        {
            HealthAddress = config?.HealthAddress ?? address
        };

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddTransient<IHealthClient, HealthClient>();
    }
}