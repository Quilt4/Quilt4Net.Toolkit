using System.Text.RegularExpressions;

namespace Quilt4Net.Toolkit.Api;

public class CompiledLoggingOptions
{
    /// <summary>Placeholder written in place of a sensitive header's value.</summary>
    public const string Mask = "***";

    public List<Regex> IncludePathRegex { get; }

    /// <summary>Whether to mask <see cref="SensitiveHeaders"/> values in logged headers.</summary>
    public bool MaskSensitiveHeaders { get; }

    /// <summary>Case-insensitive set of header names whose values are masked.</summary>
    public HashSet<string> SensitiveHeaders { get; }

    public CompiledLoggingOptions(LoggingOptions options)
    {
        IncludePathRegex = (options?.IncludePaths ?? ["^/Api"])
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList();

        MaskSensitiveHeaders = options?.MaskSensitiveHeaders ?? true;
        SensitiveHeaders = new HashSet<string>(
            options?.SensitiveHeaders ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the value to log for a header: the <see cref="Mask"/> placeholder when masking is on
    /// and the name is sensitive, otherwise the original value.
    /// </summary>
    public string MaskHeaderValue(string name, string value)
        => MaskSensitiveHeaders && SensitiveHeaders.Contains(name) ? Mask : value;
}
