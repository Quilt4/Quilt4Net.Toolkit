namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// An Atlas project credential delivered via a value-group bundle for IP-access-list (firewall)
/// management. Consumers use this credential to call Atlas's <c>/groups/{groupId}/accessList</c>
/// endpoint directly. <see cref="PrivateSecret"/> is plaintext on the wire (transport over TLS,
/// behind an API-key-gated bundle fetch).
/// </summary>
public record AtlasFirewallCredentialEntry
{
    public string Name { get; init; }

    /// <summary>
    /// <c>"ServiceAccount"</c> (OAuth2 client credentials) or <c>"ProgrammaticApiKey"</c> (HTTP Digest).
    /// The string form keeps the bundle DTO independent of the Server's enum type.
    /// </summary>
    public string CredentialType { get; init; }

    /// <summary>Public identifier — <c>PublicKey</c> for API keys, <c>ClientId</c> for Service Accounts.</summary>
    public string PublicId { get; init; }

    /// <summary>Plaintext private secret. Treat as sensitive — do not log.</summary>
    public string PrivateSecret { get; init; }

    /// <summary>Atlas project role assigned to the credential — typically <c>ProjectOwner</c>.</summary>
    public string Role { get; init; }

    /// <summary>Optional per-credential API access list (CIDRs) that limit which networks may use this credential against Atlas.</summary>
    public string[] AccessListCidrs { get; init; } = [];
}
