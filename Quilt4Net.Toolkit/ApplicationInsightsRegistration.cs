using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Quilt4Net.Toolkit.Client;

public static class ApplicationInsightsRegistration
{
    public static void AddQuilt4NetClient(this WebApplicationBuilder builder, Action<Quilt4NetOptions> options = default)
    {
        AddQuilt4NetClient(builder.Services, builder.Configuration, options);
    }

    public static void AddQuilt4NetClient(this IServiceCollection serviceCollection, IConfiguration configuration, Action<Quilt4NetOptions> options = default)
    {
        var o = BuildOptions(configuration, options);
        serviceCollection.AddSingleton(_ => o);

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