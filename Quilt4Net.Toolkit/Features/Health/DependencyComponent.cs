using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Component for Dependency check.
/// </summary>
public record DependencyComponent
{
    /// <summary>
    /// Status for the component check.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Address of the service.
    /// </summary>
    public required Uri Uri { get; init; }

    /// <summary>
    /// Extra details for the dependency checks.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, HealthComponent> DependencyComponents { get; init; }
}