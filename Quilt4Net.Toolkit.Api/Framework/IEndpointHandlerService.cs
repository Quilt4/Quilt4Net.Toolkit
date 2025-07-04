namespace Quilt4Net.Toolkit.Api.Framework;

internal interface IEndpointHandlerService
{
    Task<IResult> HandleCall(string path, string basePath, HttpContext ctx, CancellationToken cancellationToken);
}