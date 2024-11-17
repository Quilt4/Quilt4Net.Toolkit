using System.Collections.Concurrent;

namespace Quilt4Net.Toolkit.Api;

public record Quilt4NetApiOptions
{
    private readonly ConcurrentDictionary<string, Component> _components = new ();

    public bool ShowInSwagger { get; set; } = true;
    public string Pattern { get; set; } = "api";
    public string ControllerName { get; set; } = "health";

    public bool AddComponent(Component component)
    {
        if (string.IsNullOrEmpty(component.Name)) throw new ArgumentNullException(nameof(component.Name));
        if (_components.ContainsKey(component.Name)) throw new ArgumentException($"Component with name '{component.Name}' has already been added.");

        return _components.TryAdd(component.Name, component);
    }

    internal IEnumerable<Component> Components => _components.Values;
}