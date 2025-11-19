using System.Text.RegularExpressions;

namespace Quilt4Net.Toolkit.Api;

public class CompiledLoggingOptions
{
    public List<Regex> IncludePathRegex { get; }

    public CompiledLoggingOptions(LoggingOptions options)
    {
        IncludePathRegex = (options?.IncludePaths ?? ["^/Api"])
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList();
    }
}