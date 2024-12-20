﻿using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Ready.
/// </summary>
public record ReadyResponse : ResponseBase<ReadyStatus>
{
    /// <summary>
    /// Overall status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override required ReadyStatus Status { get; init; }

    /// <summary>
    /// Components that have been checked.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required Dictionary<string, ReadyComponent> Components { get; init; }
}