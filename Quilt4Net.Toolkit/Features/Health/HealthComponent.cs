﻿using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Component for Health check.
/// </summary>
public record HealthComponent
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
    public Dictionary<string, string> Details { get; init; }
}