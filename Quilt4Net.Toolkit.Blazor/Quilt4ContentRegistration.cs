using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Blazor;

public static class Quilt4ContentRegistration
{
    private static Quilt4NetServerOptions _options;

    public static IServiceCollection AddQuilt4Net(this IServiceCollection services, Func<IServiceProvider, string> environmentNameLoader, Action<Quilt4NetServerOptions> options = null)
    {
        return AddQuilt4Net(services, null, environmentNameLoader, options);
    }

    public static IServiceCollection AddQuilt4Net(this IServiceCollection services, IConfiguration configuration, Func<IServiceProvider, string> environmentNameLoader, Action<Quilt4NetServerOptions> options = null)
    {
        _options = BuildOptions(configuration, options);
        services.AddSingleton(_ => _options);
        services.AddSingleton(Options.Create(_options));

        services.AddRemoteConfiguration(environmentNameLoader);
        services.AddContent(environmentNameLoader);
        services.AddScoped<IEditContentService, EditContentService>();
        services.AddScoped<ILanguageStateService, LanguageStateService>();
        services.AddBlazoredLocalStorage();

        return services;
    }

    private static Quilt4NetServerOptions BuildOptions(IConfiguration configuration, Action<Quilt4NetServerOptions> options)
    {
        var o = configuration?.GetSection("Quilt4Net:Service").Get<Quilt4NetServerOptions>() ?? new Quilt4NetServerOptions();
        options?.Invoke(o);

        return o;
    }
}