using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.Issue;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit;

/// <summary>
/// Registration for the Quilt4Net issue tracker client and the roadmap view component.
/// </summary>
public static class IssueRegistration
{
    /// <summary>
    /// Registers the issue tracker client so <c>IssueRoadmap</c> and <see cref="IIssueService"/> can
    /// be used.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="options">Optional overrides applied after configuration is bound.</param>
    public static void AddQuilt4NetIssues(this IHostApplicationBuilder builder, Action<IssueOptions> options = null)
    {
        builder.Services.AddQuilt4NetIssues(builder.Configuration, options);
    }

    /// <summary>
    /// Registers the issue tracker client so <c>IssueRoadmap</c> and <see cref="IIssueService"/> can
    /// be used.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to bind <c>Quilt4Net:Issue</c> from.</param>
    /// <param name="options">Optional overrides applied after configuration is bound.</param>
    /// <exception cref="InvalidOperationException">The configured address is not an absolute URI.</exception>
    public static void AddQuilt4NetIssues(this IServiceCollection services, IConfiguration configuration, Action<IssueOptions> options = null)
    {
        var apiKey = configuration?.GetSection("Quilt4Net").GetSection("ApiKey").Value;
        var address = configuration?.GetSection("Quilt4Net").GetSection("Quilt4NetAddress").Value;

        var config = configuration?.GetSection("Quilt4Net:Issue").Get<IssueOptions>();
        var o = config ?? new IssueOptions();
        o.ApiKey = config?.ApiKey ?? apiKey;
        o.Quilt4NetAddress = config?.Quilt4NetAddress ?? address ?? "https://quilt4net.com/";

        options?.Invoke(o);

        // Validated after the callback, not before: the callback is what a host most often sets the
        // address from, and validating the pre-callback value lets a bad one through to
        // `new Uri(...)` in the client factory below — which then throws on the first call instead
        // of at startup, far from the line that caused it.
        if (!Uri.TryCreate(o.Quilt4NetAddress, UriKind.Absolute, out _)) throw new InvalidOperationException($"Configuration {nameof(o.Quilt4NetAddress)} with value '{o.Quilt4NetAddress}' cannot be parsed to an absolute uri.");

        services.AddSingleton(Options.Create(o));

        services.AddQuilt4NetCorrelationId();
        services.AddHttpClient(IssueService.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(o.Quilt4NetAddress);
                if (!string.IsNullOrEmpty(o.ApiKey))
                {
                    client.DefaultRequestHeaders.Remove("X-API-KEY");
                    client.DefaultRequestHeaders.Add("X-API-KEY", o.ApiKey);
                }
            })
            .AddQuilt4NetCorrelationId();

        services.AddTransient<IIssueService, IssueService>();
        services.TryAddSingleton<IConnectionService, ConnectionService>();
    }
}
