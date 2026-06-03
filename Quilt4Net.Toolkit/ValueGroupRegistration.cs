using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.ValueGroup;

namespace Quilt4Net.Toolkit;

public static class ValueGroupRegistration
{
    /// <summary>
    /// Registers <see cref="IValueGroupClient"/> against the Value Group configured in
    /// <c>Quilt4Net:ValueGroup</c>. The bound API key must carry the <c>valuegroup:read</c>
    /// scope and be tag-bound to the group on the server.
    /// </summary>
    public static IServiceCollection AddQuilt4NetValueGroupClient(this IHostApplicationBuilder builder, Action<ValueGroupClientOptions> options = null)
    {
        return builder.Services.AddQuilt4NetValueGroupClient(builder.Configuration, options);
    }

    public static IServiceCollection AddQuilt4NetValueGroupClient(this IServiceCollection services, IConfiguration configuration, Action<ValueGroupClientOptions> options = null)
    {
        var fromConfig = configuration?.GetSection("Quilt4Net:ValueGroup").Get<ValueGroupClientOptions>();
        var topLevelApiKey = configuration?.GetSection("Quilt4Net:ApiKey").Value;
        var topLevelAddress = configuration?.GetSection("Quilt4Net:Quilt4NetAddress").Value;

        var o = new ValueGroupClientOptions
        {
            Quilt4NetAddress = fromConfig?.Quilt4NetAddress ?? topLevelAddress ?? "https://quilt4net.com/",
            ApiKey = fromConfig?.ApiKey ?? topLevelApiKey,
            Ttl = fromConfig?.Ttl,
            HttpTimeout = fromConfig?.HttpTimeout ?? TimeSpan.FromSeconds(5)
        };

        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        // Named factory client: BaseAddress + X-API-KEY once, correlation-id forwarded to
        // Quilt4Net.Server. Replaces the previous per-call `new HttpClient()`.
        services.AddQuilt4NetCorrelationId();
        services.AddHttpClient(Features.ValueGroup.ValueGroupClient.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(o.Quilt4NetAddress);
                if (!string.IsNullOrEmpty(o.ApiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", o.ApiKey);
            })
            .AddQuilt4NetCorrelationId();

        services.AddSingleton<IValueGroupClient, ValueGroupClient>();
        return services;
    }
}
