using Microsoft.AspNetCore.Http;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Api.Framework;

/// <summary>
/// ASP.NET implementation of <see cref="ICorrelationIdAccessor"/> that reads the correlation id
/// <see cref="CorrelationIdMiddleware"/> stored on the current request's <c>HttpContext.Items</c>.
/// Registered by <c>AddHttpRequestLogging()</c>; replaces the base package's
/// <c>NullCorrelationIdAccessor</c> so the Toolkit's outbound HTTP clients (and any consumer client
/// opted in via <c>AddQuilt4NetCorrelationId()</c>) forward the active request's id.
/// </summary>
internal sealed class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Current
    {
        get
        {
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items != null && items.TryGetValue(CorrelationConstants.ItemKey, out var value))
            {
                return value as string;
            }

            return null;
        }
    }
}
