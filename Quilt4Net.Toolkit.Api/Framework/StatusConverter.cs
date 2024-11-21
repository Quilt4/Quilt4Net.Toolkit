﻿using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Health;
using Quilt4Net.Toolkit.Ready;

namespace Quilt4Net.Toolkit.Api.Framework;

internal static class StatusConverter
{
    public static ReadyStatus ToReadyStatusResult(this HealthStatus result)
    {
        ReadyStatus status;
        switch (result)
        {
            case HealthStatus.Healthy:
                status = ReadyStatus.Ready;
                break;
            case HealthStatus.Degraded:
                status = ReadyStatus.Degraded;
                break;
            case HealthStatus.Unhealthy:
                status = ReadyStatus.Unready;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return status;
    }

}