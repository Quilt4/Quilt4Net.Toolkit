using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit;

public static class ContentRegistration //TODO: Revisit
{
    public static void AddQuilt4NetContent(this IServiceCollection services, Func<IServiceProvider, string> environmentNameLoader)
    {
        services.AddTransient<ILanguageService, LanguageService>();
        services.AddTransient<IContentService, ContentService>();
        services.AddSingleton<IRemoteContentCallService>(s =>
        {
            var o = s.GetService<IOptions<Quilt4NetServerOptions>>();
            var name = environmentNameLoader?.Invoke(s) ?? throw new InvalidOperationException("Cannot find environment name.");
            var environmentName = new EnvironmentName { Name = name };
            var logger = s.GetService<ILogger<RemoteContentCallService>>();
            return new RemoteContentCallService(environmentName, o, logger);
        }); //NOTE: Static because it holds cached content.
    }
}