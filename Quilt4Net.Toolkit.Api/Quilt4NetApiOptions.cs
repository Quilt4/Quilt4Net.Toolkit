using System.Collections.Concurrent;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api;

/// <summary>
/// Configuration options for Quilt4NetApi.
/// </summary>
public record Quilt4NetApiOptions
{
    private readonly ConcurrentDictionary<string, Component> _components = new ();

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
        if (string.IsNullOrEmpty(component.Name)) throw new ArgumentNullException(nameof(component.Name));
        if (_components.ContainsKey(component.Name)) throw new ArgumentException($"Component with name '{component.Name}' has already been added.");

        return _components.TryAdd(component.Name, component);
    }

    internal IEnumerable<Component> Components => _components.Values;

    /// <summary>
    /// Level of detail returned when an exception occurs.
    /// Default for Production environment is Hidden.
    /// Default for Development environment is StackTrace.
    /// For all other environments default is Message.
    /// </summary>
    public ExceptionDetailLevel? ExceptionDetail { get; set; }
}