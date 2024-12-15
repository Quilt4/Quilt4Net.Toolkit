using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class HealthRegistration
{
    private static Quilt4NetHealthOptions _options;

    public static void AddQuilt4NetHealthClient(this IServiceCollection serviceCollection, Action<Quilt4NetHealthOptions> options = default)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            _options = BuildOptions(configuration, s, (_, o) => options?.Invoke(o));
            if (!_options.HealthAddress?.AbsoluteUri.EndsWith("/") ?? false)
            {
                _options.HealthAddress = new Uri($"{_options.HealthAddress.AbsoluteUri}/");
            }
            return _options;
        });

        serviceCollection.AddTransient<IHealthClient>(s =>
        {
            var o = s.GetService<Quilt4NetHealthOptions>();
            return new HealthClient(o);
        });
    }

    public static void AddQuilt4NetHealthClient(this IServiceCollection serviceCollection, Action<IServiceProvider, Quilt4NetHealthOptions> options)
    {
        serviceCollection.AddSingleton(s =>
        {
            var configuration = s.GetService<IConfiguration>();
            _options = BuildOptions(configuration, s, options);
            return _options;
        });

        serviceCollection.AddTransient<IHealthClient>(s =>
        {
            var o = s.GetService<Quilt4NetHealthOptions>();
            return new HealthClient(o);
        });
    }

    private static Quilt4NetHealthOptions BuildOptions(IConfiguration configuration, IServiceProvider s, Action<IServiceProvider, Quilt4NetHealthOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net:HealthClient").Get<Quilt4NetHealthOptions>() ?? new Quilt4NetHealthOptions();
        options?.Invoke(s, o);
        return o;
    }

    public static Quilt4NetHealthOptions GetOptions()
    {
        return _options;
    }

    public static void UseQuilt4NetHealthClient(this IServiceProvider serviceProvider)
    {
        var x = serviceProvider.GetServices<Quilt4NetHealthOptions>();
    }
}