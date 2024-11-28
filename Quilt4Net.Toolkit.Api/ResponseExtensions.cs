using Quilt4Net.Toolkit.Api.Framework;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api;

public static class ResponseExtensions
{
    public static HealthResponse ToHealthResponse(this KeyValuePair<string, HealthComponent>[] responses)
    {
        var status = responses != null && responses.Any()
            ? responses.Max(x => x.Value.Status)
            : HealthStatus.Healthy;

        var response = new HealthResponse
        {
            Status = status,
            Components = responses?.ToUniqueDictionary() ?? [],
        };

        return response;
    }

    public static ReadyResponse ToReadyResponse(this KeyValuePair<string, ReadyComponent>[] responses)
    {
        var status = responses != null && responses.Any()
            ? responses.Max(x => x.Value.Status)
            : ReadyStatus.Ready;

        var response = new ReadyResponse
        {
            Status = status,
            //Components = responses?.ToUniqueDictionary() ?? [],
            Components = responses?.ToDictionary() ?? [],
        };

        return response;
    }
}