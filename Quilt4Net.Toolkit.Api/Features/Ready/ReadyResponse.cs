using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Ready;

public abstract record ResponseBase<TStatus> where TStatus : Enum
{
    public abstract required TStatus Status { get; init; }
}

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
    public required Dictionary<string, Component> Components { get; init; }
}