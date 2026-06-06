namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// One cell in the application/environment version matrix — the latest
/// version of an application seen in a given environment, optionally pinned to a specific machine.
/// </summary>
public record VersionMatrixCell
{
    public required string ApplicationName { get; init; }
    public required string Environment { get; init; }
    public required string Version { get; init; }
    public required DateTime LastSeen { get; init; }

    /// <summary>
    /// Whether the version was identified from a Quilt4Net startup log entry
    /// (fast path) or from a general log scan (fallback).
    /// </summary>
    public required VersionMatrixSource Source { get; init; }

    /// <summary>
    /// The machine the row was observed on — typically <c>host.name</c> from the OpenTelemetry
    /// resource attributes, falling back to <c>AppRoleInstance</c>. Empty / "(unknown)" when the
    /// telemetry doesn't carry either. Aggregated <see cref="VersionMatrixView.Cells"/> rows leave
    /// this empty by convention (use <see cref="VersionMatrixView.CellsByMachine"/> for per-machine
    /// breakdown). Optional with a default so existing test fixtures don't need updating.
    /// </summary>
    public string Machine { get; init; } = "";
}

public enum VersionMatrixSource
{
    /// <summary>Identified via a Quilt4NetStartup-tagged log entry.</summary>
    Startup,

    /// <summary>Identified by scanning regular AppTraces/AppExceptions/AppRequests.</summary>
    Log,
}
