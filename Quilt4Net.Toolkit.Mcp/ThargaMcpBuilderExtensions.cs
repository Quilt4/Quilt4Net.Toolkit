using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp;

namespace Quilt4Net.Toolkit.Mcp;

/// <summary>
/// Extension methods for <see cref="IThargaMcpBuilder"/> that register the
/// Quilt4Net Application Insights MCP providers.
/// </summary>
public static class ThargaMcpBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ApplicationInsightsToolProvider"/> and
    /// <see cref="ApplicationInsightsResourceProvider"/>, exposing the
    /// Quilt4Net Application Insights query surface on the System scope.
    /// Requires <c>IApplicationInsightsService</c> to be registered (typically
    /// via <c>builder.AddQuilt4NetApplicationInsightsClient()</c>).
    /// </summary>
    /// <param name="builder">The MCP builder.</param>
    /// <param name="configure">Optional callback to configure
    /// <see cref="Quilt4NetMcpOptions"/>. If omitted, defaults are used
    /// (<see cref="DataAccessLevel.Metadata"/>, 1d default lookback, 7d cap).</param>
    public static IThargaMcpBuilder AddQuilt4Net(this IThargaMcpBuilder builder, Action<Quilt4NetMcpOptions> configure = null)
    {
        var options = new Quilt4NetMcpOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);

        builder.AddResourceProvider<ApplicationInsightsResourceProvider>();
        builder.AddToolProvider<ApplicationInsightsToolProvider>();
        return builder;
    }
}
