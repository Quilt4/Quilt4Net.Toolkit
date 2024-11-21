using Quilt4Net.Toolkit.Version;

namespace Quilt4Net.Toolkit.Api.Features.Version;

/// <summary>
/// Service for Version.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Builds the version and environment information.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}