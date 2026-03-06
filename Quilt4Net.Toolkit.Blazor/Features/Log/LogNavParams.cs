using System.Text;
using System.Text.Json;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.Log;

/// <summary>
/// Encapsulates all navigation parameters for log detail and summary views.
/// Serializes to/from a URL-safe base64 string for use as a single query parameter.
/// </summary>
public record LogNavParams
{
    public string Environment { get; init; }
    public double RangeMinutes { get; init; }
    public string Source { get; init; }
    public string Context { get; init; }
    public string Reference { get; init; }

    /// <summary>
    /// Encodes this instance to a URL-safe base64 string.
    /// </summary>
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decodes a URL-safe base64 string back to a <see cref="LogNavParams"/> instance.
    /// </summary>
    public static LogNavParams Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return new LogNavParams();

        var padded = encoded
            .Replace('-', '+')
            .Replace('_', '/');
        var padding = (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
        var bytes = Convert.FromBase64String(padded + padding);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<LogNavParams>(json) ?? new LogNavParams();
    }

    /// <summary>
    /// Creates a <see cref="LogNavParams"/> from the individual parameters used throughout the log components.
    /// </summary>
    public static LogNavParams From(string environment, TimeSpan range, LogSource? source, IApplicationInsightsContext context, string reference)
    {
        return new LogNavParams
        {
            Environment = environment,
            RangeMinutes = range.TotalMinutes,
            Source = source?.ToString(),
            Context = context?.ToKey(),
            Reference = reference
        };
    }

    public TimeSpan GetRange() => TimeSpan.FromMinutes(RangeMinutes);

    public LogSource? GetSource() =>
        string.IsNullOrEmpty(Source) ? null : Enum.Parse<LogSource>(Source);

    public IApplicationInsightsContext GetContext() =>
        ApplicationInsightsContextExtensions.ToApplicationInsightsContext(Context);
}
