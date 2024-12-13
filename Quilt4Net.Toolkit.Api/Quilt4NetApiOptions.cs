using System.Collections.Concurrent;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Configuration options for Quilt4NetApi.
/// </summary>
public record Quilt4NetApiOptions
{
    private readonly ConcurrentDictionary<string, Component> _components = new ();
    private readonly ConcurrentDictionary<Type, Type> _componentServices = new ();
    private readonly ConcurrentDictionary<string, Dependency> _dependencies = new ();

    /// <summary>
    /// If this is set to true the documentation is added to swagger.
    /// Default is true.
    /// </summary>
    public bool ShowInSwagger { get; set; } = true;

    /// <summary>
    /// Pattern to between the base address and the controller name. This value can be empty.
    /// Ex. https://localhost:7119/[Pattern]/health/live
    /// Default is api, IE. https://localhost:7119/api/health/live
    /// </summary>
    public string Pattern { get; set; } = "api";

    /// <summary>
    /// Name of the controller. This value cannot be empty.
    /// Ex. https://localhost:7119/api/[ControllerName]/live
    /// Default is health, IE. https://localhost:7119/api/health/live
    /// </summary>
    public string ControllerName { get; set; } = "Health";

    /// <summary>
    /// Assign a default action if no action is provided.
    /// Possible values are Live, Ready, Health, Metrics or Version.
    /// If set to empty string no default is routed.
    /// Default is Health.
    /// </summary>
    public string DefaultAction { get; set; } = "Health";

    /// <summary>
    /// If set to true, Ready will return 503 when the system is degraded.
    /// If set to false Ready will return 200 when the system is degraded.
    /// Default is false.
    /// </summary>
    public bool FailReadyWhenDegraded { get; set; }

    /// <summary>
    /// Add a component for perform system checks on.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public bool AddComponent(Component component)
    {
        var name = component.Name ?? string.Empty;
        if (_components.ContainsKey(name)) throw new ArgumentException($"Component with name '{name}' has already been added.");

        return _components.TryAdd(name, component);
    }

    /// <summary>
    /// External dependency to be checked in one level (Does not check dependencies on the dependency)
    /// </summary>
    /// <param name="dependency"></param>
    /// <exception cref="ArgumentException"></exception>
    public bool AddDependency(Dependency dependency)
    {
        var name = dependency.Name ?? string.Empty;
        if (_dependencies.ContainsKey(name)) throw new ArgumentException($"Dependency with name '{name}' has already been added.");

        return _dependencies.TryAdd(name, dependency);
    }

    /// <summary>
    /// Add a service that will be used for creating components.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool AddComponentService<TService>() where TService : IComponentService
    {
        if (_componentServices.ContainsKey(typeof(TService))) throw new ArgumentException($"Componentservice of type '{typeof(TService).Name}' has already been added.");

        return _componentServices.TryAdd(typeof(TService), typeof(TService));
    }

    /// <summary>
    /// Level of detail returned when an exception occurs.
    /// Default for Production environment is Hidden.
    /// Default for Development environment is StackTrace.
    /// For all other environments default is Message.
    /// </summary>
    public ExceptionDetailLevel? ExceptionDetail { get; set; }

    /// <summary>
    /// Level of detailed returned for different types of users.
    /// Default for production is AuthenticatedOnly.
    /// For all other environments default is EveryOne.
    /// </summary>
    public AuthDetailLevel? AuthDetail { get; set; }

    /// <summary>
    /// Address of check for ip address, like http://ipv4.icanhazip.com/.
    /// </summary>
    public Uri IpAddressCheckUri { get; set; }

    /// <summary>
    /// Add logger for Http request and response with body, headers, query and results.
    /// Default is append to Application Insights requests.
    /// Remember to also add 'builder.Logging.AddApplicationInsights();' at startup and add connection string, if you are using ApplicationInsights.
    /// </summary>
    public HttpRequestLogMode LogHttpRequest { get; set; } = HttpRequestLogMode.ApplicationInsights;

    /// <summary>
    /// If this is added calls to the API picks up 'X-Correlation-ID' from the header and append that to logging on the server.
    /// If there is no CorrelationId provided, one is added and returned with the response to the client.
    /// If scoped, the CorrelationId can be added by...
    /// On the client side use ...
    /// </summary>
    public bool UseCorrelationId { get; set; } = true;

    ///// <summary>
    ///// Monitor name used to track log-items to selected monitor.
    ///// If set to empty string the value will be omitted.
    ///// Default is Quilt4Net.
    ///// </summary>
    //public string MonitorName { get; set; } = Constants.Monitor;

    internal IEnumerable<Component> Components => _components.Values;
    internal IEnumerable<Type> ComponentServices => _componentServices.Keys;
    internal IEnumerable<Dependency> Dependencies => _dependencies.Values;
}