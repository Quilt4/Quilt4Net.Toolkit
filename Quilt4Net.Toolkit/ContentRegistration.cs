using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit;

public static class ContentRegistration
{
    /// <summary>
    /// Register backend usages of content from Quilt4Net.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddQuilt4NetContent(this IServiceCollection services, Action<ContentOptions> options = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        var apiKey = configuration?.GetSection("Quilt4Net").GetSection("ApiKey").Value;
        var address = configuration?.GetSection("Quilt4Net").GetSection("Quilt4NetAddress").Value;

        var config = configuration?.GetSection("Quilt4Net:Content").Get<ContentOptions>();
        var o = new ContentOptions
        {
            ApiKey = config?.ApiKey.NullIfEmpty() ?? apiKey,
            Quilt4NetAddress = config?.Quilt4NetAddress.NullIfEmpty()
                               ?? address.NullIfEmpty()
                               ?? "https://quilt4net.com/"
        };

        if (!Uri.TryCreate(o.Quilt4NetAddress, UriKind.Absolute, out _)) throw new InvalidOperationException($"Configuration {nameof(o.Quilt4NetAddress)} with value '{o.Quilt4NetAddress}' cannot be parsed to an absolute uri.");

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddTransient<ILanguageService, LanguageService>();
        services.AddTransient<IContentService, ContentService>();

        //NOTE: Holds cached content.
        services.AddSingleton<IRemoteContentCallService>(s =>
        {
            var env = s.GetService<IHostEnvironment>();
            var co = s.GetService<IOptions<ContentOptions>>();
            var environmentName = new Features.FeatureToggle.EnvironmentName { Name = env.EnvironmentName };
            var logger = s.GetService<ILogger<RemoteContentCallService>>();
            return new RemoteContentCallService(environmentName, co, logger);
        });
    }
}