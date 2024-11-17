namespace Quilt4Net.Toolkit.Api.Features.Version;

public record VersionResponse
{
    public required string Version { get; init; }
    public required string Machine { get; init; }
    public required string Environment { get; init; }
}