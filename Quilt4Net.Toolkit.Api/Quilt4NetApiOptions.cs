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
    /// Visible in OpenApi definition.
    /// Default for Production environment is false.
    /// For all other environments default is true.
    /// </summary>
    public bool? ShowInOpenApi { get; set; }

    /// <summary>
    /// This string value can be used to turn the GET, HEAD and visibility on or off for different endpoints.
    /// The values are by position Default, Live, Ready, Health, Dependencies, Metrics and Version.
    ///
    /// | Value | GET  | HEAD | Visible |
    /// | ----- | ---- | ---- | ------- |
    /// | 0     | No   | No   | No      |
    /// | 1     | Yes  | No   | No      |
    /// | 2     | No   | Yes  | No      |
    /// | 3     | Yes  | Yes  | No      |
    /// | 4     | Yes  | No   | Yes     |
    /// | 5     | No   | Yes  | Yes     |
    /// | 6     | Yes  | Yes  | Yes     |
    ///
    /// Default is 6666644
    /// </summary>
    public string Endpoints { get; set; } = "6666644";

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
    /// Possible values are Live, Ready, Health, Dependencies, Metrics or Version.
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
    /// Set this value to null, if you do not want to perform ip address check.
    /// </summary>
    public Uri IpAddressCheckUri { get; set; } = new("http://ipv4.icanhazip.com/");

    ///// <summary>
    ///// Configure logging for all requests.
    ///// To make specific configuration on controller or method level use the LoggingAttribute.
    ///// </summary>
    //public LoggingOptions Logging { get; set; } = new() { LogHttpRequest = HttpRequestLogMode.ApplicationInsights, UseCorrelationId = true };

    /// <summary>
    /// Configure certificate check.
    /// </summary>
    public CertificateCheckOptions Certificate { get; set; } = new();

    ///// <summary>
    ///// Configure the feature toggle.
    ///// </summary>
    //public FeatureToggle FeatureToggle { get; set; } = new();

    internal IEnumerable<Component> Components => _components.Values;
    internal IEnumerable<Type> ComponentServices => _componentServices.Keys;
    internal IEnumerable<Dependency> Dependencies => _dependencies.Values;
}