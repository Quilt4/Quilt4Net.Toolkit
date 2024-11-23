using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    public static void AddHealthClieht(this IServiceCollection serviceCollection, Action<IServiceProvider, Quilt4NetOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, s, options);
            return o;
        });

        serviceCollection.AddTransient<IHealthClieht>(s =>
        {
            var o = s.GetService<Quilt4NetOptions>();
            return new HealthClieht(o);
        });
    }

    public static void AddHealthClieht(this IServiceCollection serviceCollection, Action<Quilt4NetOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            var o = BuildOptions(configuration, s, (_, o) => options?.Invoke(o));
            return o;
        });

        serviceCollection.AddTransient<IHealthClieht>(s =>
        {
            var o = s.GetService<Quilt4NetOptions>();
            return new HealthClieht(o);
        });
    }

    private static Quilt4NetOptions BuildOptions(IConfiguration configuration, IServiceProvider s, Action<IServiceProvider, Quilt4NetOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net").Get<Quilt4NetOptions>() ?? new Quilt4NetOptions();
        options?.Invoke(s, o);
        return o;
    }
}