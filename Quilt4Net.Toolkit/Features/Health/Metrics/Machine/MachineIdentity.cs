using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

public record MachineIdentity
{
    public required string MachineName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
    public string Manufacturer { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Model { get; init; }

    public required MachineEnvironmentType EnvironmentType { get; init; }
}