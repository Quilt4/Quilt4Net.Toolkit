namespace Quilt4Net.Toolkit;

/// <summary>
/// Options for universal telemetry identity.
/// Configurable via code or appsettings.json at "Quilt4Net:Logging".
/// These values populate OpenTelemetry Resource attributes and are attached to traces, logs and metrics automatically.
/// </summary>
public record Quilt4NetLoggingOptions
{
    /// <summary>
    /// Application name used to identify this application in telemetry.
    /// Maps to OpenTelemetry resource attribute <c>service.name</c>
    /// (surfaces as <c>cloud_RoleName</c> in Application Insights).
    /// Default is the entry assembly name.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Application version.
    /// Maps to OpenTelemetry resource attribute <c>service.version</c>
    /// (surfaces as <c>application_Version</c> in Application Insights).
    /// Default is the entry assembly version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Environment name (e.g. Development, Staging, Production).
    /// Maps to OpenTelemetry resource attribute <c>deployment.environment</c>
    /// (surfaces under <c>customDimensions</c> in Application Insights).
    /// Default resolution: IHostEnvironment.EnvironmentName → DOTNET_ENVIRONMENT → ASPNETCORE_ENVIRONMENT → "Production".
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// Identifier for the instrumentation source emitting the telemetry.
    /// Surfaces as <c>customDimensions["quilt4net.monitor"]</c> on every trace, exception
    /// and request, so KQL queries can scope to telemetry produced by Quilt4Net (or by a
    /// per-deployment override) without parsing message text. Default is <c>"Quilt4Net"</c>.
    /// </summary>
    public string MonitorName { get; set; } = "Quilt4Net";

    /// <summary>
    /// Logical instance identifier used to disambiguate multiple deployments of the same
    /// compiled service (same <c>service.name</c>) — for example a multi-tenanted or
    /// multi-branded deployment of one binary. Maps to OpenTelemetry resource attribute
    /// <c>service.instance.id</c> (surfaces as <c>cloud_RoleInstance</c> and
    /// <c>customDimensions["service.instance.id"]</c> in Application Insights).
    ///
    /// Resolution order if this property is null:
    /// <list type="number">
    /// <item><c>OTEL_RESOURCE_ATTRIBUTES</c> env var (parsed for <c>service.instance.id=...</c>).</item>
    /// <item><c>QUILT4NET_SERVICE_INSTANCE_ID</c> env var.</item>
    /// <item>Falls back to <c>MachineName</c> on the OTel resource (today's behaviour) and
    ///       <i>absent</i> from per-record customDimensions — no breaking change for existing consumers.</item>
    /// </list>
    /// </summary>
    public string ServiceInstanceId { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), a logged exception's <see cref="System.Exception.Data"/>
    /// entries are copied onto the exception telemetry so they surface under
    /// <c>customDimensions</c> in Application Insights. This makes an id attached to the exception
    /// — e.g. <c>e.AddData("CorrelationId", guid)</c> — queryable, for example:
    /// <code>AppExceptions | where tostring(customDimensions.CorrelationId) == "&lt;guid&gt;"</code>
    /// Enrichment is applied to both the OpenTelemetry pipeline (via a log processor) and the
    /// classic Application Insights SDK (via an <c>ITelemetryInitializer</c> on
    /// <c>ExceptionTelemetry</c>). Set to <c>false</c> to opt out.
    /// </summary>
    public bool EnrichExceptionData { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, values from <see cref="Microsoft.Extensions.Logging.ILogger"/> scopes
    /// (e.g. <c>logger.BeginScope(...)</c>) are captured and copied onto each log record so they
    /// surface under <c>customDimensions</c> in Application Insights. This is what makes the
    /// <c>CorrelationId</c> that <c>CorrelationIdMiddleware</c> pushes as a scope actually
    /// queryable on every <c>AppTrace</c> / <c>AppException</c> raised during the request.
    /// Default is <c>false</c> (opt-in) because scope capture increases telemetry volume; enable it
    /// deliberately with <c>AddQuilt4NetLogging(o =&gt; o.IncludeScopes = true)</c>.
    /// </summary>
    public bool IncludeScopes { get; set; }
}
