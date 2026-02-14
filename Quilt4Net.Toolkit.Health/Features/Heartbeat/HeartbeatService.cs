using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
    private readonly string _name;
    private static string _version;

    public HeartbeatService(TelemetryClient telemetryClient, IHealthService healthService, IMetricsService metricsService, IVersionService versionService)
    {
        _telemetryClient = telemetryClient;
        _healthService = healthService;
        _metricsService = metricsService;
        _versionService = versionService;
        _name = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
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
}