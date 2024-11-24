using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    public static void AddHealthClieht(this IServiceCollection serviceCollection, Action<IServiceProvider, Quilt4NetHealthOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, s, options);
            return o;
        });

        serviceCollection.AddTransient<IHealthClieht>(s =>
        {
            var o = s.GetService<Quilt4NetHealthOptions>();
            return new HealthClieht(o);
        });
    }

    public static void AddHealthClieht(this IServiceCollection serviceCollection, Action<Quilt4NetHealthOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, s, (_, o) => options?.Invoke(o));
            return o;
        });

        serviceCollection.AddTransient<IHealthClieht>(s =>
        {
            var o = s.GetService<Quilt4NetHealthOptions>();
            return new HealthClieht(o);
        });
    }

    private static Quilt4NetHealthOptions BuildOptions(IConfiguration configuration, IServiceProvider s, Action<IServiceProvider, Quilt4NetHealthOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net").Get<Quilt4NetHealthOptions>() ?? new Quilt4NetHealthOptions();
        options?.Invoke(s, o);
        return o;
    }
}