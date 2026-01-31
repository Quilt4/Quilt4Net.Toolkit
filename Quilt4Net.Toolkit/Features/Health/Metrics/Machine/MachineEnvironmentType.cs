using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health.Metrics.Machine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MachineEnvironmentType
{
    Physical,
    VirtualMachine,
    Container
}