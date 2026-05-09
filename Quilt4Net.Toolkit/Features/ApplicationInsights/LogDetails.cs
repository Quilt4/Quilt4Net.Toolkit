namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogDetails : LogItemBase
{
    public required LogSource Source { get; init; }
    public required SeverityLevel SeverityLevel { get; init; }

    /// <summary>
    /// Per-record correlation id read from the row's <c>customDimensions["CorrelationId"]</c>.
    /// Populated automatically by <c>Quilt4Net.Toolkit.Api.CorrelationIdMiddleware</c> when an
    /// upstream caller sets the <c>X-Correlation-ID</c> header (or the middleware mints one).
    /// Empty string when the row has no correlation id.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Application version read with the same coalesce as the read-side environment lookup:
    /// OTel resource attribute <c>service.version</c> first (set by <c>AddQuilt4NetLogging</c>),
    /// then a legacy <c>Version</c> scope tag, then the AI built-in <c>AppVersion</c> column.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The full row from <c>pack_all()</c>, deserialised into a dictionary. Used by the
    /// <c>LogDetailContent</c> Details and Stack Trace tabs to render structured key/value
    /// pairs and the parsed stack frames respectively.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Raw { get; init; }

    /// <summary>
    /// The same row as <see cref="Raw"/> but as a JSON string — what the <c>LogDetailContent</c>
    /// Raw tab renders inside its <c>&lt;pre&gt;</c> block (and what its CopyButton copies).
    /// </summary>
    public required string RawJson { get; init; }
}