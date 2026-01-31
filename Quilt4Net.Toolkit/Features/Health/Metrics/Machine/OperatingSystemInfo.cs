using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

public record OperatingSystemInfo
{
    public required string Platform { get; init; }           // Windows / Linux

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Distribution { get; init; }                // Ubuntu, RHEL, etc.

    public required string Version { get; init; }
    public required int AddressSizeBits { get; init; }        // 32 / 64
}