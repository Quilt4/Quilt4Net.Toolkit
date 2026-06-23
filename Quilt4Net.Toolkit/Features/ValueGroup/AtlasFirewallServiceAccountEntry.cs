namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// An Atlas <b>Service Account</b> credential (OAuth2 client credentials) delivered via a value-group
/// bundle for IP-access-list (firewall) management. Authenticate to the Atlas Admin API by exchanging
/// <see cref="ClientId"/> + <see cref="ClientSecret"/> for a Bearer access token, then call
/// <c>/api/atlas/v2/groups/{GroupId}/accessList</c>. <see cref="ClientSecret"/> is plaintext on the
/// wire (transport over TLS, behind an API-key-gated bundle fetch).
/// </summary>
public record AtlasFirewallServiceAccountEntry
{
    public string Name { get; init; }

    /// <summary>The Atlas project (group) this credential manages.</summary>
    public string GroupId { get; init; }

    /// <summary>OAuth2 client id (the public identifier).</summary>
    public string ClientId { get; init; }

    /// <summary>OAuth2 client secret. Treat as sensitive — do not log.</summary>
    public string ClientSecret { get; init; }

    /// <summary>Atlas project role assigned to the Service Account — typically <c>ProjectOwner</c>.</summary>
    public string Role { get; init; }
}
