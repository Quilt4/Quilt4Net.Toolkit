using System.Text;

namespace Quilt4Net.Toolkit.Features.ReleaseNotes;

/// <summary>
/// A parsed SemVer 2.0 version.
/// </summary>
/// <remarks>
/// Release notes are keyed by version and read as "everything newer than the one I last showed",
/// so ordering is the whole feature — and text ordering gets it wrong: <c>1.10.0</c> sorts
/// <em>before</em> <c>1.9.0</c> as a string. This type is the single place that ordering is
/// defined. The toolkit uses it to decide whether a caller-supplied version is even worth a round
/// trip; Quilt4Net.Server uses the same code to validate a write and to build the sort key its
/// delta query ranges over. One implementation, so the client and the server cannot come to
/// different conclusions about what "newer" means.
/// </remarks>
public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    /// <summary>
    /// Width each numeric component is zero-padded to in <see cref="SortKey"/>. Ten digits keeps
    /// every component the same length, which is what lets an ordinal string comparison — and
    /// therefore an indexed range query in the database — reproduce numeric ordering.
    /// </summary>
    private const int PadWidth = 10;

    /// <summary>Largest value a component may hold, given <see cref="PadWidth"/>.</summary>
    private const long MaxComponent = 9_999_999_999;

    private SemanticVersion(string original, long major, long minor, long patch, string preRelease, string build, string sortKey)
    {
        Original = original;
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        Build = build;
        SortKey = sortKey;
    }

    /// <summary>The version exactly as it was written, which is what gets displayed back.</summary>
    public string Original { get; }

    public long Major { get; }
    public long Minor { get; }
    public long Patch { get; }

    /// <summary>Pre-release identifiers without the leading hyphen, or <c>null</c> for a release.</summary>
    public string PreRelease { get; }

    /// <summary>Build metadata without the leading plus, or <c>null</c>. Ignored for precedence, per the spec.</summary>
    public string Build { get; }

    /// <summary>True when this is a pre-release, which the delta leaves out unless asked for it.</summary>
    public bool IsPreRelease => PreRelease != null;

    /// <summary>
    /// Fixed-width encoding whose ordinal string order is SemVer precedence order. Persisted on the
    /// row so "every note newer than X" is an indexed range scan rather than loading a team's whole
    /// history and sorting it in memory.
    /// </summary>
    public string SortKey { get; }

    public static SemanticVersion Parse(string value)
        => TryParse(value, out var version)
            ? version
            : throw new FormatException($"'{value}' is not a SemVer 2.0 version. Expected MAJOR.MINOR.PATCH, optionally followed by -prerelease and +build (for example 1.4.0 or 2.0.0-beta.1).");

    /// <summary>
    /// Strict SemVer 2.0. Deliberately refuses the near-misses — a leading <c>v</c>, a two-part
    /// <c>1.2</c>, a four-part <c>1.2.3.4</c>, leading zeros — because the alternative is accepting
    /// them and then ordering them by guesswork.
    /// </summary>
    public static bool TryParse(string value, out SemanticVersion version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var original = value.Trim();
        var remainder = original;

        string build = null;
        var plus = remainder.IndexOf('+');
        if (plus >= 0)
        {
            build = remainder[(plus + 1)..];
            remainder = remainder[..plus];
            if (!IsValidDotSeparated(build, allowLeadingZeroNumerics: true)) return false;
        }

        string preRelease = null;
        var hyphen = remainder.IndexOf('-');
        if (hyphen >= 0)
        {
            preRelease = remainder[(hyphen + 1)..];
            remainder = remainder[..hyphen];
            if (!IsValidDotSeparated(preRelease, allowLeadingZeroNumerics: false)) return false;
        }

        var core = remainder.Split('.');
        if (core.Length != 3) return false;
        if (!TryParseComponent(core[0], out var major)) return false;
        if (!TryParseComponent(core[1], out var minor)) return false;
        if (!TryParseComponent(core[2], out var patch)) return false;

        version = new SemanticVersion(original, major, minor, patch, preRelease, build, BuildSortKey(major, minor, patch, preRelease));
        return true;
    }

    private static bool TryParseComponent(string value, out long component)
    {
        component = 0;
        if (value.Length == 0) return false;
        if (value.Length > 1 && value[0] == '0') return false; // leading zeros are not SemVer
        foreach (var c in value) if (!char.IsAsciiDigit(c)) return false;
        return long.TryParse(value, out component) && component <= MaxComponent;
    }

    private static bool IsValidDotSeparated(string value, bool allowLeadingZeroNumerics)
    {
        if (value.Length == 0) return false;

        foreach (var identifier in value.Split('.'))
        {
            if (identifier.Length == 0) return false;
            foreach (var c in identifier) if (!char.IsAsciiLetterOrDigit(c) && c != '-') return false;
            if (!allowLeadingZeroNumerics && IsNumeric(identifier) && identifier.Length > 1 && identifier[0] == '0') return false;
        }

        return true;
    }

    private static bool IsNumeric(string identifier)
    {
        if (identifier.Length == 0) return false;
        foreach (var c in identifier) if (!char.IsAsciiDigit(c)) return false;
        return true;
    }

    /// <summary>
    /// The encoding, and why each part of it is shaped the way it is:
    /// <list type="bullet">
    /// <item>Components are padded to a fixed width, so an ordinal comparison is a numeric comparison.</item>
    /// <item>A release ends in <c>~</c> and a pre-release in <c>-</c>. <c>-</c> sorts below <c>~</c>,
    /// which is the spec's rule that <c>1.0.0-beta</c> precedes <c>1.0.0</c>.</item>
    /// <item>Each pre-release identifier is tagged <c>0</c> when numeric and <c>1</c> otherwise, so a
    /// numeric identifier always precedes an alphanumeric one however it is spelled — the spec's
    /// rule 11, which a naive encoding gets wrong for identifiers such as <c>-x</c>.</item>
    /// </list>
    /// </summary>
    private static string BuildSortKey(long major, long minor, long patch, string preRelease)
    {
        var sb = new StringBuilder()
            .Append(major.ToString().PadLeft(PadWidth, '0')).Append('.')
            .Append(minor.ToString().PadLeft(PadWidth, '0')).Append('.')
            .Append(patch.ToString().PadLeft(PadWidth, '0'));

        if (preRelease == null) return sb.Append('~').ToString();

        sb.Append('-');
        var first = true;
        foreach (var identifier in preRelease.Split('.'))
        {
            if (!first) sb.Append('.');
            first = false;
            if (IsNumeric(identifier) && identifier.Length <= PadWidth) sb.Append('0').Append(identifier.PadLeft(PadWidth, '0'));
            else sb.Append('1').Append(identifier);
        }

        return sb.ToString();
    }

    public int CompareTo(SemanticVersion other) => other == null ? 1 : string.CompareOrdinal(SortKey, other.SortKey);

    /// <summary>Precedence equality: build metadata is ignored, as the spec says it must be.</summary>
    public bool Equals(SemanticVersion other) => other != null && SortKey == other.SortKey;

    public override bool Equals(object obj) => Equals(obj as SemanticVersion);
    public override int GetHashCode() => SortKey.GetHashCode();
    public override string ToString() => Original;

    public static bool operator <(SemanticVersion left, SemanticVersion right) => Compare(left, right) < 0;
    public static bool operator >(SemanticVersion left, SemanticVersion right) => Compare(left, right) > 0;
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => Compare(left, right) <= 0;
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => Compare(left, right) >= 0;
    public static bool operator ==(SemanticVersion left, SemanticVersion right) => Compare(left, right) == 0;
    public static bool operator !=(SemanticVersion left, SemanticVersion right) => Compare(left, right) != 0;

    private static int Compare(SemanticVersion left, SemanticVersion right)
        => left is null ? (right is null ? 0 : -1) : left.CompareTo(right);
}
