using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Blazor.Features.Configuration;

/// <summary>
/// Scope presentation and filtering for <c>RemoteConfigurationAdmin</c>.
/// <para>
/// A configuration entry is scoped by application, environment and instance, and the admin grid lists
/// every entry the team's API key can read — so the same key legitimately appears once per
/// application × environment × instance. Without the scope on screen those read as duplicate rows,
/// while the row's own edit/reset/delete act on a scope the operator was never shown.
/// </para>
/// <para>
/// <see cref="ConfigurationResponse"/> declares <c>Application</c>, <c>Environment</c> and
/// <c>Instance</c> as <c>required string</c>, but a <b>shared</b> entry legitimately arrives with them
/// null — which is why <c>RemoteConfigCallService.GetAllAsync</c> does no filtering of its own. Every
/// member here treats null and empty alike.
/// </para>
/// </summary>
internal static class ConfigurationScope
{
    /// <summary>
    /// Shown in place of a null/empty application or environment, so a shared entry reads as
    /// deliberately unscoped rather than as a blank cell.
    /// </summary>
    public const string SharedLabel = "(all)";

    /// <summary>Label for the dropdown option that applies no environment filter.</summary>
    public const string AllEnvironmentsLabel = "All environments";

    /// <summary>Renders a scope value for display, marking the shared case explicitly.</summary>
    public static string Label(string value) => string.IsNullOrEmpty(value) ? SharedLabel : value;

    /// <summary>
    /// The environments available to filter on, in display order — every distinct environment present
    /// in <paramref name="entries"/>. Shared entries contribute nothing: they are not an environment,
    /// and they are never filtered out (see <see cref="MatchesEnvironment"/>).
    /// </summary>
    public static string[] AvailableEnvironments(IEnumerable<ConfigurationResponse> entries)
    {
        return entries
            .Select(x => x.Environment)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// The environment to select on first load: the host's own environment when it has entries,
    /// otherwise no filter at all.
    /// </summary>
    /// <remarks>
    /// <c>LogEnvironmentSelector</c> falls back to the first available option, but that view is
    /// read-only. Here an arbitrary first-option default would hide configuration the operator can
    /// edit — the precise failure this grid exists to fix — so the fallback is "show everything".
    /// </remarks>
    public static string DefaultEnvironment(IEnumerable<string> available, string hostEnvironment)
    {
        if (string.IsNullOrEmpty(hostEnvironment)) return null;

        return available.FirstOrDefault(x => string.Equals(x, hostEnvironment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether an entry survives the environment filter.
    /// </summary>
    /// <param name="entryEnvironment">The entry's environment; null/empty means shared.</param>
    /// <param name="selectedEnvironment">The selected environment; null means no filter.</param>
    /// <remarks>
    /// A shared entry applies to <i>every</i> environment, so it always passes. Filtering it out would
    /// make the filter conceal more configuration than the missing columns ever did.
    /// </remarks>
    public static bool MatchesEnvironment(string entryEnvironment, string selectedEnvironment)
    {
        if (string.IsNullOrEmpty(selectedEnvironment)) return true;
        if (string.IsNullOrEmpty(entryEnvironment)) return true;

        return string.Equals(entryEnvironment, selectedEnvironment, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the Instance column is worth rendering — true when any entry carries an instance.
    /// </summary>
    /// <remarks>
    /// Instance is a level slated for removal (<c>configuration-content-ui-spec.md</c>; the Server's
    /// own grid already has it commented out), so it gets no permanent column. It must still be
    /// visible when set, or two entries differing only by instance are indistinguishable again.
    /// <para>
    /// Computed over the full loaded set rather than the filtered rows, so the column does not appear
    /// and vanish as the operator changes the filter.
    /// </para>
    /// </remarks>
    public static bool ShowInstance(IEnumerable<ConfigurationResponse> entries)
    {
        return entries.Any(x => !string.IsNullOrEmpty(x.Instance));
    }
}
