using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit;

public static class RemoteConfigurationRegistration //TODO: Revisit
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
        }); //NOTE: Static because it holds cached toggles.
        services.AddTransient<IFeatureToggleService, FeatureToggleService>();
        services.AddTransient<IRemoteConfigurationService, RemoteConfigurationService>();
    }
}