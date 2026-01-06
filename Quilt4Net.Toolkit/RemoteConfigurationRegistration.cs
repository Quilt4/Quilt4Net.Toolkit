using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit;

public static class RemoteConfigurationRegistration
{
    /// <summary>
    /// Register backend usages of remote configuration and feature toggles from Quilt4Net.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddQuilt4NetRemoteConfiguration(this IServiceCollection services, Action<RemoteConfigurationOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        var apiKey = configuration?.GetSection("Quilt4Net").GetSection("ApiKey").Value;
        var address = configuration?.GetSection("Quilt4Net").GetSection("Quilt4NetAddress").Value;

        var config = configuration?.GetSection("Quilt4Net:RemoteConfiguration").Get<RemoteConfigurationOptions>();
        var o = new RemoteConfigurationOptions
        {
            ApiKey = config?.ApiKey ?? apiKey,
            Quilt4NetAddress = config?.Quilt4NetAddress ?? address ?? "https://quilt4net.com/"
        };

        if (!Uri.TryCreate(o.Quilt4NetAddress, UriKind.Absolute, out _)) throw new InvalidOperationException($"Configuration {nameof(o.Quilt4NetAddress)} with value '{o.Quilt4NetAddress}' cannot be parsed to an absolute uri.");

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        //NOTE: Holds cached content.
        services.AddSingleton<IRemoteConfigCallService>(s =>
        {
            var env = s.GetService<IHostEnvironment>();
            var ro = s.GetService<IOptions<RemoteConfigurationOptions>>();
            var environmentName = new Features.FeatureToggle.EnvironmentName { Name = env.EnvironmentName };
            var logger = s.GetService<ILogger<RemoteConfigCallService>>();
            return new RemoteConfigCallService(s, environmentName, ro, logger);
        });
        services.AddTransient<IFeatureToggleService, FeatureToggleService>();
        services.AddTransient<IRemoteConfigurationService, RemoteConfigurationService>();
    }
}