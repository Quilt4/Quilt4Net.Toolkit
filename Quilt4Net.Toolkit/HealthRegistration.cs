using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Api.Features.FeatureToggle;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit;

public static class ContentRegistration
{
    public static void AddContent(this IServiceCollection services, Func<IServiceProvider, string> environmentNameLoader)
    {
        services.AddTransient<ILanguageService, LanguageService>();
        services.AddTransient<IContentService, ContentService>();
        services.AddTransient<IRemoteContentCallService>(s =>
        {
            var o = s.GetService<IOptions<Quilt4NetServerOptions>>();
            var name = environmentNameLoader?.Invoke(s) ?? throw new InvalidOperationException("Cannot find environment name.");
            var environmentName = new EnvironmentName { Name = name };
            var logger = s.GetService<ILogger<RemoteContentCallService>>();
            return new RemoteContentCallService(s, environmentName, o, logger);
        });
    }
}

public static class RemoteConfigurationRegistration
{
    public static void AddRemoteConfiguration(this IServiceCollection services, Func<IServiceProvider, string> environmentNameLoader)
    {
        services.AddSingleton<IRemoteConfigCallService>(s =>
        {
            var o = s.GetService<IOptions<Quilt4NetServerOptions>>();
            var name = environmentNameLoader?.Invoke(s) ?? throw new InvalidOperationException("Cannot find environment name.");
            var environmentName = new EnvironmentName { Name = name };
            var logger = s.GetService<ILogger<RemoteConfigCallService>>();
            return new RemoteConfigCallService(s, environmentName, o, logger);
        });  //NOTE: Static because it holds cached toggles
        services.AddTransient<IFeatureToggleService, FeatureToggleService>();
        services.AddTransient<IRemoteConfigurationService, RemoteConfigurationService>();
    }
}

public static class HealthRegistration
{
    private static Quilt4NetHealthOptions _options;

    public static void AddQuilt4NetHealthClient(this IServiceCollection serviceCollection, Action<Quilt4NetHealthOptions> options = null)
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