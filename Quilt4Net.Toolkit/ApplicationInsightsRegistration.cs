using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class ApplicationInsightsRegistration
{
    public static void AddQuilt4NetApplicationInsights(this IServiceCollection serviceCollection, Action<Quilt4NetApplicationInsightsOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, options);
            return o;
        });
        serviceCollection.AddTransient<IApplicationInsightsService, ApplicationInsightsService>();
        serviceCollection.AddTransient<IHealthClient, HealthClient>();
    }

    private static Quilt4NetApplicationInsightsOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetApplicationInsightsOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net:ApplicationInsights").Get<Quilt4NetApplicationInsightsOptions>() ?? new Quilt4NetApplicationInsightsOptions();
        options?.Invoke(o);
        return o;
    }
}