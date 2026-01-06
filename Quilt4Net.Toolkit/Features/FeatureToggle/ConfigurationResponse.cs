namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record ConfigurationResponse : IConfigValue
{
    public required string Key { get; init; }
    public required string Application { get; init; }
    public required string Environment { get; init; }
    public required string Instance { get; init; }
    public required string Value { get; init; }
    public required string DefaultValue { get; init; }
    public required string ValueType { get; init; }
    public required DateTime? LastUsed { get; init; }
    public required TimeSpan? Ttl { get; init; }
}