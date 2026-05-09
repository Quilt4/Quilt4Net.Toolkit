using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogItem : LogItemBase
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    /// <summary>
    /// Per-record correlation id read from the log entry's <c>customDimensions["CorrelationId"]</c>.
    /// Empty string when the row has no correlation id (typical for telemetry from outside the
    /// Quilt4Net pipeline). For Quilt4Net-instrumented apps, populated automatically by
    /// <c>CorrelationIdMiddleware</c>'s logging scope.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;
}