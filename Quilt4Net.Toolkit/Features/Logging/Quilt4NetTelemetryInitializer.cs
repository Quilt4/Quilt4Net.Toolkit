using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Quilt4Net.Toolkit.Features.Logging;

internal class Quilt4NetTelemetryInitializer : ITelemetryInitializer
{
    private readonly Quilt4NetLoggingOptions _options;

    public Quilt4NetTelemetryInitializer(Quilt4NetLoggingOptions options)
    {
        _options = options;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (!string.IsNullOrEmpty(_options.ApplicationName))
        {
            telemetry.Context.Cloud.RoleName = _options.ApplicationName;
        }

        if (!string.IsNullOrEmpty(_options.Version))
        {
            telemetry.Context.Component.Version = _options.Version;
        }

        if (!string.IsNullOrEmpty(_options.Environment))
        {
            telemetry.Context.GlobalProperties["Environment"] = _options.Environment;
        }
    }
}
