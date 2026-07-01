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
            yield return new KeyValuePair<string, string>(entry.Key, ToInvariantString(entry.Value));
        }
    }

    public static string ToInvariantString(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);
}

/// <summary>
/// Appends string key/value pairs onto a <see cref="LogRecord"/> without overwriting attributes
/// that are already present (identity attributes, exception data, or earlier scopes).
/// </summary>
internal static class LogRecordAttributes
{
    public static void AppendNew(LogRecord data, IEnumerable<KeyValuePair<string, string>> additions)
    {
        var existing = data.Attributes;
        var list = new List<KeyValuePair<string, object>>(existing ?? []);
        var keys = new HashSet<string>(list.Count);
        foreach (var attribute in list) keys.Add(attribute.Key);

        var appended = false;
        foreach (var addition in additions)
        {
            if (!keys.Add(addition.Key)) continue;
            list.Add(new KeyValuePair<string, object>(addition.Key, addition.Value));
            appended = true;
        }

        if (appended) data.Attributes = list;
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
        LogRecordAttributes.AppendNew(data, ExceptionDataEnricher.GetStringData(data.Exception));
    }
}

/// <summary>
/// Copies <see cref="Microsoft.Extensions.Logging.ILogger"/> scope values (captured when
/// <see cref="Quilt4NetLoggingOptions.IncludeScopes"/> is enabled) onto the <see cref="LogRecord"/>
/// attributes so scoped context — notably the <c>CorrelationId</c> pushed by
/// <c>CorrelationIdMiddleware</c> — reaches <c>customDimensions</c>. The message template's
/// <c>{OriginalFormat}</c> entry is skipped; existing attributes are never overwritten.
/// </summary>
internal sealed class ScopeAttributesLogProcessor : BaseProcessor<LogRecord>
{
    private const string OriginalFormatKey = "{OriginalFormat}";

    public override void OnEnd(LogRecord data)
    {
        var scopeValues = new List<KeyValuePair<string, string>>();

        data.ForEachScope(static (scope, state) =>
        {
            foreach (var pair in scope)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Key == OriginalFormatKey || pair.Value is null) continue;
                state.Add(new KeyValuePair<string, string>(pair.Key, ExceptionDataEnricher.ToInvariantString(pair.Value)));
            }
        }, scopeValues);

        if (scopeValues.Count == 0) return;
        LogRecordAttributes.AppendNew(data, scopeValues);
    }
}
