namespace Quilt4Net.Toolkit.Framework;

/// <summary>
/// Response from the WhoAmI endpoint containing team info and API key capabilities.
/// </summary>
public record WhoAmIResponse
{
    public string TeamKey { get; init; }
    public string TeamName { get; init; }
    public string AccessLevel { get; init; }
    public string[] Scopes { get; init; } = [];

    public bool HasScope(string scope) => Scopes.Contains(scope);
}
