namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Resolves the correlation id of the current ambient context, used by the Toolkit's outbound HTTP
/// clients to forward it to Quilt4Net.Server so one id spans the whole call chain.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// The current correlation id, or <c>null</c> when there is none to propagate. A non-null value
    /// is the id that <c>Quilt4Net.Toolkit.Api.Framework.CorrelationIdMiddleware</c> placed on the
    /// active request (HttpContext); outside an HTTP request there is nothing to forward.
    /// </summary>
    string Current { get; }
}
