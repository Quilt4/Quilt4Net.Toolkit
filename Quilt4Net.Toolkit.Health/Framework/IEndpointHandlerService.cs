using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Health.Framework;

internal interface IEndpointHandlerService
{
    Task<IResult> HandleCall<T>(HealthEndpoint healthEndpoint, HttpContext ctx, T options, CancellationToken cancellationToken) where T : MethodOptions;
}