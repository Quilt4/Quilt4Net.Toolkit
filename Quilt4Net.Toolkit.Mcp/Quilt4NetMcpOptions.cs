namespace Quilt4Net.Toolkit.Mcp;

/// <summary>
/// Options consumed by the Quilt4Net.Toolkit.Mcp providers. Configured via
/// <see cref="ThargaMcpBuilderExtensions.AddQuilt4Net"/> inside the
/// <c>AddThargaMcp</c> callback.
/// </summary>
public sealed class Quilt4NetMcpOptions
{
    /// <summary>
    /// Maximum sensitivity of data the tools will return.
    /// Defaults to <see cref="DataAccessLevel.Metadata"/> (no log content).
    /// Set to <see cref="DataAccessLevel.DataRead"/> to enable search /
    /// detail / summary / lookup tools.
    /// </summary>
    public DataAccessLevel DataAccess { get; set; } = DataAccessLevel.Metadata;

    /// <summary>
    /// Lookback window applied when a tool call doesn't specify
    /// <c>lookbackHours</c>. Default <c>1d</c>.
    /// </summary>
    public TimeSpan DefaultLookback { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Maximum lookback window the tools will accept; <c>lookbackHours</c>
    /// values larger than this are clamped server-side. Default <c>7d</c>.
    /// </summary>
    public TimeSpan MaxLookback { get; set; } = TimeSpan.FromDays(7);
}
