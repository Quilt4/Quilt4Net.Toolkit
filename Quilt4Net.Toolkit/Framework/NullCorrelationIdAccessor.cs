namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Fallback <see cref="ICorrelationIdAccessor"/> that never has an id to propagate. Registered by
/// the base (ASP.NET-free) Toolkit so <see cref="CorrelationIdHandler"/> always resolves an
/// accessor; an ASP.NET host replaces it with one that reads <c>HttpContext</c>
/// (<c>Quilt4Net.Toolkit.Api</c>'s <c>HttpContextCorrelationIdAccessor</c>). With this in place a
/// WPF/console consumer can attach the handler without error — it simply forwards nothing.
/// </summary>
internal sealed class NullCorrelationIdAccessor : ICorrelationIdAccessor
{
    public string Current => null;
}
