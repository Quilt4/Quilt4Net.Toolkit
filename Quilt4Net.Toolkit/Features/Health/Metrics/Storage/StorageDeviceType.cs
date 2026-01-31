using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.Health.Metrics.Storage;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageDeviceType
{
    Local,
    Network,
    Removable,
    Virtual
}