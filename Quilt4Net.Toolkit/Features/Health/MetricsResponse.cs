using System.Text.Json.Serialization;
using Quilt4Net.Toolkit.Features.Health.Metrics.Machine;
using Quilt4Net.Toolkit.Features.Health.Metrics.Storage;

namespace Quilt4Net.Toolkit.Features.Health;

/// <summary>
/// Response for Metrics.
/// </summary>
public record MetricsResponse
{
    /// <summary>
    /// The time that the application has been running.
    /// </summary>
    public required TimeSpan ApplicationUptime { get; init; }

    /// <summary>
    /// Information about the machine and OS.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Machine Machine { get; set; }

    /// <summary>
    /// Memory information.
    /// </summary>
    public required Memory Memory { get; init; }

    /// <summary>
    /// Processor information.
    /// </summary>
    public required Processor Processor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Storage Storage { get; init; }

    /// <summary>
    /// GPU information.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Gpu Gpu { get; init; }

    /// <summary>
    /// The time it took to get the information.
    /// </summary>
    public required TimeSpan Elapsed { get; init; }
}