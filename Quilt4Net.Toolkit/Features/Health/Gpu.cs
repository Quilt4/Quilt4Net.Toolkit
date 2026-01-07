using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

public record Gpu
{
    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? VideoMemoryGb { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? CoreClockGHz { get; init; }
}