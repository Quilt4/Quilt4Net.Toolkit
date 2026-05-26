namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Plain key/value entry inside a <see cref="ValueGroupBundle"/>. Values are transported as-is
/// (no encryption). Future feature extensions (an <c>IsSecret</c> flag, encryption at rest,
/// masking) are planned in a coordinated follow-up that also addresses today's
/// <c>ApplicationInsightsConfigurationResponse.ClientSecret</c> plain-text storage.
/// </summary>
public record KeyValueEntry
{
    public string Name { get; init; }
    public string Value { get; init; }
    public string Description { get; init; }
}
