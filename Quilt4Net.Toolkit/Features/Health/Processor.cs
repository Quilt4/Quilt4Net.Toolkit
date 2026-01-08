using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Processor information
/// </summary>
public record Processor
{
    /// <summary>
    /// Total CPU-time for the process.
    /// </summary>
    public required TimeSpan CpuTime { get; init; }

    /// <summary>
    /// GHz-hours for the process.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? TotalGHzHours { get; init; }

    /// <summary>
    /// Total number of cores on the machine.
    /// </summary>
    public required int NumberOfCores { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? PhysicalCpuCores { get; init; }

    /// <summary>
    /// Processor speed on the machine.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? ProcessorSpeedGHz { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? CurrentCpuSpeedGHz { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? L3CacheMb { get; init; }
}