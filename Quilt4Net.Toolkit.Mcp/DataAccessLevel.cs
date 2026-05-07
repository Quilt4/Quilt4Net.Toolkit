namespace Quilt4Net.Toolkit.Mcp;

/// <summary>
/// Controls how much Application Insights data the MCP surface exposes.
/// Each level is a strict superset of the previous one. Mirrors
/// <c>Tharga.MongoDB.Mcp.DataAccessLevel</c> by intent.
/// </summary>
public enum DataAccessLevel
{
    /// <summary>
    /// Default. Only metadata is exposed — environment names and resource
    /// listings — nothing that returns log content.
    /// </summary>
    Metadata = 0,

    /// <summary>
    /// Adds tools that return actual log content: search, detail, summary,
    /// and the incident / correlation lookup tools.
    /// </summary>
    DataRead = 1,

    /// <summary>
    /// Reserved for a future write phase (e.g. cache invalidation, content
    /// refresh). No tools at this level are emitted yet.
    /// </summary>
    DataReadWrite = 2,
}
