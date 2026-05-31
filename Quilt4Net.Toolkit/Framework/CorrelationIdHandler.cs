namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// A <see cref="DelegatingHandler"/> that forwards the ambient correlation id on outbound HTTP
/// calls. Attach it to any HttpClient (via <c>IHttpClientBuilder.AddQuilt4NetCorrelationId()</c>)
/// whose target should receive the same <c>X-Correlation-ID</c> as the current request — letting
/// one id span the consuming app, the services it calls, and Quilt4Net.Server.
/// </summary>
/// <remarks>
/// No-op when there is no ambient id (<see cref="ICorrelationIdAccessor.Current"/> is null/empty)
/// or when the request already carries the header — so an explicitly-set id is never overwritten.
/// </remarks>
public sealed class CorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationIdAccessor _accessor;

    public CorrelationIdHandler(ICorrelationIdAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _accessor?.Current;
        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(CorrelationConstants.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
