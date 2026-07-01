using System.Globalization;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Quilt4Net.Toolkit.Features.Measure;

namespace Quilt4Net.Toolkit.Features.Logging;

/// <summary>
/// Shared extraction of an exception's <see cref="Exception.Data"/> entries as stringified
/// key/value pairs. Values are rendered with <see cref="CultureInfo.InvariantCulture"/> so a
/// correlation id (or any datum) reads identically regardless of the host's locale, and to match
/// the string-only shape of Application Insights <c>customDimensions</c>. Null keys and values are
/// skipped.
/// </summary>
internal static class ExceptionDataEnricher
{
    public static IEnumerable<KeyValuePair<string, string>> GetStringData(Exception exception)
    {
        foreach (var entry in exception.GetData())
        {
            if (string.IsNullOrEmpty(entry.Key) || entry.Value is null) continue;
            yield return new KeyValuePair<string, string>(entry.Key, Convert.ToString(entry.Value, CultureInfo.InvariantCulture));
        }
    }
}

/// <summary>
/// Copies a logged exception's <see cref="Exception.Data"/> entries onto the <see cref="LogRecord"/>
/// attributes so they reach <c>customDimensions</c> via the Azure Monitor OpenTelemetry exporter.
/// Existing attributes (including the Quilt4Net identity attributes) are never overwritten.
/// </summary>
internal sealed class ExceptionDataLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (data.Exception is null) return;

        var existing = data.Attributes;
        var list = new List<KeyValuePair<string, object>>(existing ?? []);
        var keys = new HashSet<string>(list.Count);
        foreach (var attribute in list) keys.Add(attribute.Key);

        foreach (var entry in ExceptionDataEnricher.GetStringData(data.Exception))
        {
            if (!keys.Add(entry.Key)) continue;
            list.Add(new KeyValuePair<string, object>(entry.Key, entry.Value));
        }

        data.Attributes = list;
    }
}
