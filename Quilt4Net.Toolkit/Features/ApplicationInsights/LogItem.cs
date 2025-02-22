﻿using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

//TODO: Add item Id ResourceGUID, IKey
public record LogItem
{
    //public required string Id { get; init; }
    public required string SummaryIdentifier { get; init; }
    //public required string CorrelationId { get; init; }
    public required string Application { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogType Type { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }
    public required string Message { get; init; }
}