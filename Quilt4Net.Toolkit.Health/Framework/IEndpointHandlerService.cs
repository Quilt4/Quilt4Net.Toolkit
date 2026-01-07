using Quilt4Net.Toolkit.Features.Api;

namespace Quilt4Net.Toolkit.Health.Framework;

internal interface IEndpointHandlerService
{
    Task<IResult> HandleCall(HealthEndpoint healthEndpoint, HttpContext ctx, CancellationToken cancellationToken);
}