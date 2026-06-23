namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// An Atlas <b>classic / legacy programmatic API key</b> (HTTP Digest) delivered via a value-group
/// bundle for IP-access-list (firewall) management. Authenticate to the Atlas Admin API with HTTP
/// Digest, using <see cref="PublicKey"/> as the username and <see cref="PrivateKey"/> as the password,
/// then call <c>/api/atlas/v2/groups/{GroupId}/accessList</c>. <see cref="PrivateKey"/> is plaintext on
/// the wire (transport over TLS, behind an API-key-gated bundle fetch).
/// </summary>
public record AtlasFirewallApiKeyEntry
{
    public string Name { get; init; }

    /// <summary>The Atlas project (group) this key manages.</summary>
    public string GroupId { get; init; }

    /// <summary>Programmatic API key public key (the HTTP Digest username).</summary>
    public string PublicKey { get; init; }

    /// <summary>Programmatic API key private key (the HTTP Digest password). Treat as sensitive — do not log.</summary>
    public string PrivateKey { get; init; }

    /// <summary>Atlas project role assigned to the key — typically <c>ProjectOwner</c>.</summary>
    public string Role { get; init; }

    /// <summary>Optional API access list (CIDRs) limiting which networks may use this key against Atlas. Empty = unrestricted.</summary>
    public string[] AccessListCidrs { get; init; } = [];
}
