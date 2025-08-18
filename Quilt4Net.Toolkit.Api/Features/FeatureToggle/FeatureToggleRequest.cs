namespace Quilt4Net.Toolkit.Api.Features.FeatureToggle;

public record FeatureToggleRequest : IKeyContext
{
    public required string Key { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string Version { get; init; }
    public required string DefaultValue { get; init; }
    public required string ValueType { get; init; }
    public TimeSpan? Ttl { get; init; }
}

public record ContentRequest
{
}