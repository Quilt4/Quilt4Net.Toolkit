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
    /// <param name="builder"></param>
    /// <param name="options"></param>
    public static void AddQuilt4NetContent(this IHostApplicationBuilder builder, Action<ContentOptions> options = null)
    {
        builder.Services.AddQuilt4NetContent(builder.Configuration, options);
    }

    /// <summary>
    /// Register backend usages of content from Quilt4Net.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="options"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddQuilt4NetContent(this IServiceCollection services, IConfiguration configuration, Action<ContentOptions> options = null)
    {
        var apiKey = configuration?.GetSection("Quilt4Net").GetSection("ApiKey").Value;
        var address = configuration?.GetSection("Quilt4Net").GetSection("Quilt4NetAddress").Value;

        // Bind the whole section so EVERY appsettings field flows through (HttpTimeout, Ttl,
        // FailureCacheDuration, Application, StaleWhileRevalidate, …). Only ApiKey / Quilt4NetAddress
        // keep their special fallback to the top-level Quilt4Net:ApiKey / :Quilt4NetAddress (and the
        // default address). Referencing the nullable `config` (not the bound object's defaulted
        // properties) preserves the exact original precedence — incl. top-level address when no
        // Quilt4Net:Content section is present.
        var config = configuration?.GetSection("Quilt4Net:Content").Get<ContentOptions>();
        var o = config ?? new ContentOptions();
        o.ApiKey = config?.ApiKey.NullIfEmpty() ?? apiKey;
        o.Quilt4NetAddress = config?.Quilt4NetAddress.NullIfEmpty() ?? address.NullIfEmpty() ?? "https://quilt4net.com/";

        if (!Uri.TryCreate(o.Quilt4NetAddress, UriKind.Absolute, out _)) throw new InvalidOperationException($"Configuration {nameof(o.Quilt4NetAddress)} with value '{o.Quilt4NetAddress}' cannot be parsed to an absolute uri.");

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddTransient<ILanguageService, LanguageService>();
        services.AddTransient<IContentService, ContentService>();

        // Named factory client: BaseAddress + X-API-KEY configured once, correlation-id forwarded
        // to Quilt4Net.Server. Replaces the previous per-call `new HttpClient()` (socket-pooling +
        // correlation propagation). Captures o directly — options are fixed at registration.
        services.AddQuilt4NetCorrelationId();
        services.AddHttpClient(RemoteContentCallService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(o.Quilt4NetAddress);
                if (!string.IsNullOrEmpty(o.ApiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", o.ApiKey);
            })
            .AddQuilt4NetCorrelationId();

        //NOTE: Holds cached content.
        services.AddSingleton<IRemoteContentCallService>(s =>
        {
            var env = s.GetService<IHostEnvironment>();
            var co = s.GetService<IOptions<ContentOptions>>();
            var environmentName = new Features.FeatureToggle.EnvironmentName { Name = env?.EnvironmentName ?? "Production" };
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var logger = s.GetService<ILogger<RemoteContentCallService>>();
            return new RemoteContentCallService(environmentName, co, httpClientFactory, logger);
        });

        // Same factory pattern as RemoteContentCallService — EnvironmentName isn't a globally
        // registered type, it's resolved per-component from IHostEnvironment. Singleton so the
        // reader can hold caches later (Phase 2 keeps it stateless).
        services.AddSingleton<Features.Content.Pages.IContentPageReader>(s =>
        {
            var env = s.GetService<IHostEnvironment>();
            var co = s.GetService<IOptions<ContentOptions>>();
            var environmentName = new Features.FeatureToggle.EnvironmentName { Name = env?.EnvironmentName ?? "Production" };
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var logger = s.GetService<ILogger<Features.Content.Pages.RemoteContentPageReader>>();
            return new Features.Content.Pages.RemoteContentPageReader(environmentName, co, httpClientFactory, logger);
        });
    }
}