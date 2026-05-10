using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Quilt4Net.Toolkit.Features.Logging;

/// <summary>
/// The identity attributes attached to every Quilt4Net-instrumented log record and
/// activity. Computed once at registration time from <see cref="Quilt4NetLoggingOptions"/>
/// plus <see cref="System.Environment.MachineName"/>; the processors below copy the values
/// onto each record/span at export time. <see cref="ServiceInstanceId"/> is optional —
/// when null no <c>service.instance.id</c> attribute is emitted per-record (today's behaviour);
/// when set the same value is also stamped on the OTel resource by the registration helper.
/// </summary>
internal sealed record TelemetryIdentity(
    string Environment,
    string ApplicationName,
    string Version,
    string MachineName,
    string MonitorName,
    string ServiceInstanceId = null);

/// <summary>
/// Adds the five <see cref="TelemetryIdentity"/> attributes to every <see cref="LogRecord"/>
/// (AppTraces, AppExceptions). Resource attributes alone don't reach per-row Properties via
/// the Azure Monitor exporter, so we copy them onto the record.
/// </summary>
internal sealed class TelemetryIdentityLogProcessor : BaseProcessor<LogRecord>
{
    private readonly TelemetryIdentity _identity;

    public TelemetryIdentityLogProcessor(TelemetryIdentity identity)
    {
        _identity = identity;
    }

    public override void OnEnd(LogRecord data)
    {
        var existing = data.Attributes;
        var capacity = (existing?.Count ?? 0) + 6;
        var list = new List<KeyValuePair<string, object>>(capacity);
        if (existing != null) list.AddRange(existing);

        if (!string.IsNullOrEmpty(_identity.Environment)) list.Add(new("deployment.environment", _identity.Environment));
        if (!string.IsNullOrEmpty(_identity.ApplicationName)) list.Add(new("service.name", _identity.ApplicationName));
        if (!string.IsNullOrEmpty(_identity.Version)) list.Add(new("service.version", _identity.Version));
        if (!string.IsNullOrEmpty(_identity.MachineName)) list.Add(new("host.name", _identity.MachineName));
        if (!string.IsNullOrEmpty(_identity.MonitorName)) list.Add(new("quilt4net.monitor", _identity.MonitorName));
        if (!string.IsNullOrEmpty(_identity.ServiceInstanceId)) list.Add(new("service.instance.id", _identity.ServiceInstanceId));

        data.Attributes = list;
    }
}

/// <summary>
/// Adds the five <see cref="TelemetryIdentity"/> attributes to every <see cref="Activity"/>
/// (AppRequests + outbound dependencies). <c>SetTag</c> is the OTel-native way to add per-
/// span attributes; the Azure Monitor exporter forwards them into <c>customDimensions</c>.
/// </summary>
internal sealed class TelemetryIdentityActivityProcessor : BaseProcessor<Activity>
{
    private readonly TelemetryIdentity _identity;

    public TelemetryIdentityActivityProcessor(TelemetryIdentity identity)
    {
        _identity = identity;
    }

    public override void OnEnd(Activity data)
    {
        if (!string.IsNullOrEmpty(_identity.Environment)) data.SetTag("deployment.environment", _identity.Environment);
        if (!string.IsNullOrEmpty(_identity.ApplicationName)) data.SetTag("service.name", _identity.ApplicationName);
        if (!string.IsNullOrEmpty(_identity.Version)) data.SetTag("service.version", _identity.Version);
        if (!string.IsNullOrEmpty(_identity.MachineName)) data.SetTag("host.name", _identity.MachineName);
        if (!string.IsNullOrEmpty(_identity.MonitorName)) data.SetTag("quilt4net.monitor", _identity.MonitorName);
        if (!string.IsNullOrEmpty(_identity.ServiceInstanceId)) data.SetTag("service.instance.id", _identity.ServiceInstanceId);
    }
}
