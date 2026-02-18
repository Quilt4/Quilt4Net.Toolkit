using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Features.Health.Metrics;
using Quilt4Net.Toolkit.Features.Health.Version;

namespace Quilt4Net.Toolkit.Health.Features.Heartbeat;

internal class HeartbeatService : IHeartbeatService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IHealthService _healthService;
    private readonly IMetricsService _metricsService;
    private readonly IVersionService _versionService;
    private readonly ILogger<HeartbeatService> _logger;
    private readonly string _name;
    private static string _version;

    public HeartbeatService(IHealthService healthService, IMetricsService metricsService, IVersionService versionService, ILogger<HeartbeatService> logger, HeartbeatOptions heartbeatOptions, TelemetryClient telemetryClient = null)
    {
        _telemetryClient = telemetryClient ?? CreateFromOptions(heartbeatOptions);
        _healthService = healthService;
        _metricsService = metricsService;
        _versionService = versionService;
        _logger = logger;
        _name = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_telemetryClient == null)
        {
            _logger.LogWarning("Heartbeat execution skipped because no TelemetryClient is configured. Register Application Insights or set Heartbeat.ConnectionString to enable heartbeat telemetry.");
            return;
        }

        var statuses = await _healthService.GetStatusAsync(cancellationToken: cancellationToken).ToArrayAsync(cancellationToken);
        var statusType = $"{statuses.Max(x => x.Value.Status)}".ToLower();
        var healthy = statuses.All(x => x.Value.Status != HealthStatus.Unhealthy);

        var metrics = await _metricsService.GetMetricsAsync(cancellationToken);
        _version ??= (await _versionService.GetVersionAsync(cancellationToken))?.Version;

        var availabilityTelemetry = new AvailabilityTelemetry
        {
            Name = _name,
            Message = $"The service is {statusType}.",
            Properties =
            {
                { "version", _version },
                { "totalGHzHours", $"{metrics.Processor.TotalGHzHours}" },
                { "applicationMemoryUsageGb", $"{metrics.Memory.ApplicationMemoryUsageGb}" },
                { "uptimeHours", $"{metrics.ApplicationUptime.TotalHours}" },
            },
            RunLocation = Environment.MachineName,
            Timestamp = DateTimeOffset.UtcNow,
            Success = healthy
        };

        foreach (var status in statuses)
        {
            status.Value.Details.TryGetValue("message", out var message);
            availabilityTelemetry.Properties.TryAdd(status.Key, $"{status.Value.Status} ({message})");
        }

        _telemetryClient.TrackAvailability(availabilityTelemetry);
    }

    private static TelemetryClient CreateFromOptions(HeartbeatOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString)) return null;

        var config = new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
        {
            ConnectionString = options.ConnectionString
        };
        return new TelemetryClient(config);
    }
}