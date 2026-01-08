using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Memory information
/// </summary>
public record Memory
{
    /// <summary>
    /// Memory usage of the application, in GB.
    /// </summary>
    public required double ApplicationMemoryUsageGb { get; init; }

    /// <summary>
    /// Total memory of the machine, in GB.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? TotalMemoryGb { get; init; }

    /// <summary>
    /// Available free memmory of the machine, in GB.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? AvailableFreeMemoryGb { get; init; }
}