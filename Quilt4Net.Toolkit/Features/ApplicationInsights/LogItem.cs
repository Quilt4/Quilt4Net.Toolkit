//using System.Text.Json.Serialization;

//namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

//public record LogItem : LogItemBase
//{
//    public required string Id { get; init; }
//    public required string Fingerprint { get; init; }
//    public required DateTime TimeGenerated { get; init; }

//    [JsonConverter(typeof(JsonStringEnumConverter))]
//    public required LogSource Source { get; init; }

//    [JsonConverter(typeof(JsonStringEnumConverter))]
//    public required SeverityLevel SeverityLevel { get; init; }

//    public required string Message { get; init; }
//    public required string Environment { get; init; }
//    public required string Application { get; init; }
//}