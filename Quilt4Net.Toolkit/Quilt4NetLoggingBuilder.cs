using Microsoft.Extensions.DependencyInjection;

namespace Quilt4Net.Toolkit;

/// <summary>
/// Builder returned by AddQuilt4NetLogging. Extension packages (e.g. Quilt4Net.Toolkit.Api)
/// can add extension methods on this type to register additional capabilities.
/// </summary>
public class Quilt4NetLoggingBuilder
{
    /// <summary>
    /// The service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// The resolved logging options.
    /// </summary>
    public Quilt4NetLoggingOptions Options { get; }

    internal Quilt4NetLoggingBuilder(IServiceCollection services, Quilt4NetLoggingOptions options)
    {
        Services = services;
        Options = options;
    }
}
