using System.Text.RegularExpressions;

namespace Quilt4Net.Toolkit.Api;

public class CompiledLoggingOptions
{
    public List<Regex> IncludePathRegex { get; }

    public CompiledLoggingOptions(Quilt4NetApiOptions options)
    {
        IncludePathRegex = (options?.Logging?.IncludePaths ?? ["^/Api"])
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList();
    }
}