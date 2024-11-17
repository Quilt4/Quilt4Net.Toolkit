using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public record Processor
{
    public required TimeSpan CpuTime { get; init; }
    public required double TotalGHzHours { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required int NumberOfCores { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required double ProcessorSpeedGHz { get; init; }
}