namespace Quilt4Net.Toolkit.Features.Atlas;

/// <summary>
/// Thrown when a firewall call is rejected with 401/403 — the key was revoked, lacks the required
/// firewall scope, or targets a group it is not bound to. Distinct from transient HTTP failures so
/// callers can treat it as a configuration/authorization problem rather than retry.
/// </summary>
public sealed class AtlasFirewallAuthorizationException : Exception
{
    public AtlasFirewallAuthorizationException(string message) : base(message) { }
}
