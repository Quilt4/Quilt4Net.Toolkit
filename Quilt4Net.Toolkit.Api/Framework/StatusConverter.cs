using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Ready;

namespace Quilt4Net.Toolkit.Api.Framework;

internal static class StatusConverter
{
    public static ReadyStatusResult ToReadyStatusResult(this HealthStatusResult result)
    {
        ReadyStatusResult status;
        switch (result)
        {
            case HealthStatusResult.Healthy:
                status = ReadyStatusResult.Ready;
                break;
            case HealthStatusResult.Degraded:
                status = ReadyStatusResult.Degraded;
                break;
            case HealthStatusResult.Unhealthy:
                status = ReadyStatusResult.Unready;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return status;
    }

}