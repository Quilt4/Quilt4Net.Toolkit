using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public record Memory
{
    public required double ApplicationMemoryUsageMB { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required double AvailableFreeMemoryMB { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required double TotalMemoryMB { get; init; }
}