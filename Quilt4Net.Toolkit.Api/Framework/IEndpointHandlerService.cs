using Quilt4Net.Toolkit.Api.Framework.Endpoints;

namespace Quilt4Net.Toolkit.Api.Framework;

internal interface IEndpointHandlerService
{
    Task<IResult> HandleCall(HealthEndpoint healthEndpoint, HttpContext ctx, CancellationToken cancellationToken);
}