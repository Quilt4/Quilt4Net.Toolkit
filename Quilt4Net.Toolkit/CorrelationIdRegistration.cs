using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit;

/// <summary>
/// Registration for forwarding the ambient Quilt4Net correlation id (<c>X-Correlation-ID</c>) on a
/// consuming application's own outbound HTTP calls, so one id spans the app, the internal services
/// it calls, and Quilt4Net.Server.
/// </summary>
public static class CorrelationIdRegistration
{
    /// <summary>
    /// Registers the correlation-id propagation services: <see cref="CorrelationIdHandler"/> and a
    /// fallback <see cref="ICorrelationIdAccessor"/> (which forwards nothing). In an ASP.NET host,
    /// <c>AddQuilt4NetLogging().AddHttpRequestLogging()</c> (Quilt4Net.Toolkit.Api) replaces the
    /// accessor with one that reads the current request's correlation id from <c>HttpContext</c>.
    /// </summary>
    /// <remarks>
    /// This call alone does not attach propagation to any HttpClient. Opt a specific client in with
    /// <see cref="AddQuilt4NetCorrelationId(IHttpClientBuilder)"/> — propagation is per-client by
    /// design so an internal id is never leaked to third-party endpoints.
    /// </remarks>
    public static IServiceCollection AddQuilt4NetCorrelationId(this IServiceCollection services)
    {
        // Fallback accessor — overridden by the ASP.NET HttpContext-backed accessor when
        // Quilt4Net.Toolkit.Api is wired up. TryAdd so an already-registered accessor wins.
        services.TryAddSingleton<ICorrelationIdAccessor, NullCorrelationIdAccessor>();

        // The handler is resolved per HttpClient; transient is the required lifetime for a
        // DelegatingHandler used via AddHttpMessageHandler.
        services.TryAddTransient<CorrelationIdHandler>();

        return services;
    }

    /// <summary>
    /// Opts a named/typed HttpClient into correlation-id propagation by attaching
    /// <see cref="CorrelationIdHandler"/>. Use on clients that call correlation-aware (typically your
    /// own internal) services:
    /// <code>services.AddHttpClient("internal-api").AddQuilt4NetCorrelationId();</code>
    /// Ensures the supporting services are registered, so a single call is enough.
    /// </summary>
    public static IHttpClientBuilder AddQuilt4NetCorrelationId(this IHttpClientBuilder builder)
    {
        builder.Services.AddQuilt4NetCorrelationId();
        return builder.AddHttpMessageHandler<CorrelationIdHandler>();
    }
}
