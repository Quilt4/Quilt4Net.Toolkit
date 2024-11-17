using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Health;

/// <summary>
/// Component for Health check.
/// </summary>
public record Component
{
    /// <summary>
    /// Status for the component check.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Extra details for the health checks.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, string> Details { get; init; }
}