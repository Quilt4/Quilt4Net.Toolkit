using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Metrics;

/// <summary>
/// Memory information
/// </summary>
public record Memory
{
    /// <summary>
    /// Memory usage of the application, in MB.
    /// </summary>
    public required double ApplicationMemoryUsageMb { get; init; }

    /// <summary>
    /// Available free memmory of the machine, in MB.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required double AvailableFreeMemoryMb { get; init; }

    /// <summary>
    /// Total memory of the machine, in MB.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required double TotalMemoryMb { get; init; }
}