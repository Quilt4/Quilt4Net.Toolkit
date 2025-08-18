using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Blazor;

public static class Quilt4ContentRegistration
{
    private static Quilt4NetContentOptions _options;

    public static IServiceCollection AddQuilt4NetContent(this IServiceCollection services, Action<Quilt4NetContentOptions> options = null)
    {
        return AddQuilt4NetContent(services, null, options);
    }

    public static IServiceCollection AddQuilt4NetContent(this IServiceCollection services, IConfiguration configuration, Action<Quilt4NetContentOptions> options = null)
    {
        _options = BuildOptions(configuration, options);
        services.AddSingleton(_ => _options);
        services.AddSingleton(Options.Create(_options));

        services.AddTransient<ILanguageService, LanguageService>();

        return services;
    }

    private static Quilt4NetContentOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetContentOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net:Content").Get<Quilt4NetContentOptions>() ?? new Quilt4NetContentOptions();
        options?.Invoke(o);

        return o;
    }

}