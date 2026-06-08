using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record SummaryData
{
    public required string Fingerprint { get; init; }

    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string Application { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LogSource Source { get; init; }

    public required Item[] Items { get; init; }

    /// <summary>
    /// Total number of rows that matched the fingerprint in the lookback window — including any
    /// rows clipped by the <c>maxItems</c> cap. Lets the UI surface "showing X of Y" so the
    /// operator knows the list is truncated. Equals <c>Items.Length</c> when no clipping happened.
    /// </summary>
    public long TotalCount { get; init; }

    public record Item
    {
        public required string Id { get; init; }
        public required DateTime TimeGenerated { get; init; }
        public required string Message { get; init; }
    }
}