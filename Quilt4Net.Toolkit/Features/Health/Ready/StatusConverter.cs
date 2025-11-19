namespace Quilt4Net.Toolkit.Features.Health.Ready;

internal static class StatusConverter
{
    public static ReadyStatus ToReadyStatusResult(this HealthStatus healthStatus)
    {
        switch (healthStatus)
        {
            case HealthStatus.Healthy:
                return ReadyStatus.Ready;
            case HealthStatus.Degraded:
                return ReadyStatus.Degraded;
            case HealthStatus.Unhealthy:
                return ReadyStatus.Unready;
            default:
                throw new ArgumentOutOfRangeException(nameof(healthStatus), healthStatus, null);
        }
    }
}