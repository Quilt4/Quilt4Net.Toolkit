namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

/// <summary>
/// Static map entry for grouping multiple raw application names (cloud_RoleName)
/// under one logical name. Configured via <see cref="ApplicationInsightsOptions.ApplicationAlias"/>;
/// applied by <c>VersionMatrixDisplay</c> (in <c>Quilt4Net.Toolkit.Blazor</c>) when no
/// per-component <c>AliasFolder</c> delegate is supplied.
/// </summary>
public sealed record ApplicationAliasMap
{
    public required string LogicalName { get; init; }
    public required string[] SourceNames { get; init; }
}
