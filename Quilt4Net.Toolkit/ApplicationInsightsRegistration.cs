using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class ApplicationInsightsRegistration
{
    public static void AddQuilt4NetClient(this IServiceCollection serviceCollection, Action<Quilt4NetOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, options);
            return o;
        });
        serviceCollection.AddTransient<IApplicationInsightsClient, ApplicationInsightsClient>();
        serviceCollection.AddTransient<IHealthClieht, HealthClieht>();
    }

    private static Quilt4NetOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net").Get<Quilt4NetOptions>() ?? new Quilt4NetOptions();
        options?.Invoke(o);
        return o;
    }
}