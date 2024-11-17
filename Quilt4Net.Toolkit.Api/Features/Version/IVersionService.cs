namespace Quilt4Net.Toolkit.Api.Features.Version;

public interface IVersionService
{
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}