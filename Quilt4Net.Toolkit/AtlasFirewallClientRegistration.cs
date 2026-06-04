using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Features.Atlas;

namespace Quilt4Net.Toolkit;

public static class AtlasFirewallClientRegistration
{
    /// <summary>
    /// Registers <see cref="IAtlasFirewallClientFactory"/>, which builds an
    /// <see cref="IAtlasFirewallClient"/> for a firewall key obtained from a value-group bundle.
    /// The Quilt4Net server address comes from <c>Quilt4Net:Quilt4NetAddress</c> (default
    /// <c>https://quilt4net.com/</c>); the per-call API key and group ride on each bundle entry.
    /// </summary>
    public static IServiceCollection AddQuilt4NetAtlasFirewallClient(this IHostApplicationBuilder builder, Action<AtlasFirewallClientOptions> options = null)
        => builder.Services.AddQuilt4NetAtlasFirewallClient(builder.Configuration, options);

    public static IServiceCollection AddQuilt4NetAtlasFirewallClient(this IServiceCollection services, IConfiguration configuration, Action<AtlasFirewallClientOptions> options = null)
    {
        var topLevelAddress = configuration?.GetSection("Quilt4Net:Quilt4NetAddress").Value;

        var o = new AtlasFirewallClientOptions
        {
            Quilt4NetAddress = topLevelAddress ?? "https://quilt4net.com/",
        };
        options?.Invoke(o);

        services.AddQuilt4NetCorrelationId();
        services.AddHttpClient(AtlasFirewallClientFactory.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(o.Quilt4NetAddress);
                client.Timeout = o.HttpTimeout;
            })
            .AddQuilt4NetCorrelationId();

        services.AddSingleton<IAtlasFirewallClientFactory, AtlasFirewallClientFactory>();
        return services;
    }
}
