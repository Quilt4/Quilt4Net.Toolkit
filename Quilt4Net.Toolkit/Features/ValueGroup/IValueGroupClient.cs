namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Fetches the configured Value Group's bundle from Quilt4Net.Server. One client = one group
/// (the group id is bound at registration). Apps that need multiple groups register multiple
/// clients via keyed services.
/// </summary>
public interface IValueGroupClient
{
    /// <summary>
    /// Fetches the bundle, returning cached values when available. Throws
    /// <see cref="ValueGroupAuthorizationException"/> on 401/403 (revoked key, wrong scope, or
    /// the key is no longer bound to this group). Transient HTTP failures return the last-cached
    /// bundle if any, otherwise rethrow the underlying exception.
    /// </summary>
    Task<ValueGroupBundle> GetAsync(CancellationToken cancellationToken = default);
}
