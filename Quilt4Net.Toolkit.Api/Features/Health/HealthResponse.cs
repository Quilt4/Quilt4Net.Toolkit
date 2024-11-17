using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Health;

/// <summary>
/// Response for Health.
/// </summary>
public record HealthResponse
{
    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Components that have been checked.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, Component> Components { get; init; }
}