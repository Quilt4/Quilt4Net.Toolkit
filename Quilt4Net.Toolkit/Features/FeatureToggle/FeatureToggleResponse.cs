namespace Quilt4Net.Toolkit.Features.FeatureToggle;

public record FeatureToggleResponse
{
    /// <summary>
    /// Value of the feature toggle.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// After this time the client will automatically check for new values.
    /// </summary>
    public required DateTime ValidTo { get; init; }
}