using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

/// <summary>
/// Multi-select filter state for the Log Summary and Search views. An empty array on a
/// dimension means "no filter on this dimension" (show all values). Used both as the in-memory
/// state of <see cref="LogFilterBar"/> and as the persisted shape in browser localStorage.
/// </summary>
public sealed record LogFilters
{
    public LogSource[] Sources { get; init; } = [];
    public SeverityLevel[] Levels { get; init; } = [];

    /// <summary>Logical application names (post-alias resolution) the user wants to keep.</summary>
    public string[] Applications { get; init; } = [];

    public string[] Environments { get; init; } = [];

    public static LogFilters Empty { get; } = new();

    public bool IsEmpty
        => Sources.Length == 0
           && Levels.Length == 0
           && Applications.Length == 0
           && Environments.Length == 0;

    /// <summary>True when <paramref name="value"/> passes this filter on the given dimension.</summary>
    public bool MatchesSource(LogSource value) => Sources.Length == 0 || Array.IndexOf(Sources, value) >= 0;
    public bool MatchesLevel(SeverityLevel value) => Levels.Length == 0 || Array.IndexOf(Levels, value) >= 0;
    public bool MatchesApplication(string logical) => Applications.Length == 0 || Array.IndexOf(Applications, logical ?? string.Empty) >= 0;
    public bool MatchesEnvironment(string env) => Environments.Length == 0 || Array.IndexOf(Environments, env ?? string.Empty) >= 0;
}
