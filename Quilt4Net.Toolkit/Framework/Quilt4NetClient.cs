using System.Reflection;

namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Identifies this Toolkit to the Quilt4Net server on every outgoing call.
/// </summary>
/// <remarks>
/// Phase 1 of the compatibility mechanic. Until now a Toolkit call carried only <c>X-API-KEY</c>, so
/// the server had no way to know which Toolkit a consumer was running — and therefore no way to
/// answer "is anyone still on the old contract" other than by assumption.
/// <para>
/// Sent as a header on <b>every</b> request rather than through a greeting call. A greeting cannot
/// carry enforcement: you cannot refuse a call on the strength of a handshake that may never have
/// happened, and a restart, a new process instance or a cached client skips it. A header makes the
/// record exact per call and lets a refusal be decided on the call actually being made.
/// </para>
/// </remarks>
internal static class Quilt4NetClient
{
    /// <summary>The header carrying <c>&lt;name&gt;/&lt;version&gt;</c>, e.g. <c>Quilt4Net.Toolkit/1.0.12</c>.</summary>
    public const string HeaderName = "X-Quilt4Net-Client";

    /// <summary>Computed once — the value cannot change within a process.</summary>
    public static readonly string Value = Build();

    private static string Build()
    {
        var assembly = typeof(Quilt4NetClient).Assembly;
        var name = assembly.GetName().Name ?? "Quilt4Net.Toolkit";
        return $"{name}/{Version(assembly)}";
    }

    /// <summary>
    /// The informational version with any build metadata removed.
    /// </summary>
    /// <remarks>
    /// SourceLink appends <c>+&lt;commit&gt;</c>, which must not reach the wire: the server records
    /// what it receives, so a per-commit value would give the compatibility ledger one row per build
    /// rather than one per release, and the question being asked is about released contracts.
    /// Falls back to the assembly version, and then to <c>unknown</c> — an unidentified client is a
    /// fact worth recording rather than a reason to fail a call.
    /// </remarks>
    private static string Version(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus < 0 ? informational : informational[..plus];
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}
