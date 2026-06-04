namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// A Quilt4Net firewall API key delivered in a <see cref="ValueGroupBundle"/>. It lets a consumer open
/// the Atlas firewall (and/or report usage) for one Atlas project <b>through the Quilt4Net proxy</b> —
/// the Atlas Project-Owner Service Account never leaves the server, and no Atlas credential is handed
/// out. Build a typed client with <c>IAtlasFirewallClientFactory.Create(entry)</c>.
/// </summary>
public record AtlasFirewallProxyKeyEntry
{
    public string Name { get; init; }

    /// <summary>
    /// The Quilt4Net API key (bearer for the firewall endpoints). Treat as a secret — do not log.
    /// Carries <c>firewall:manage</c> when <see cref="CanManage"/> is true (open/close), otherwise
    /// <c>firewall:usage</c> (report-used / state only).
    /// </summary>
    public string ApiKey { get; init; }

    /// <summary>The Atlas project (group) this key is bound to; sent with every firewall call.</summary>
    public string GroupId { get; init; }

    /// <summary>True if the key can open/close the firewall; false if it can only report usage.</summary>
    public bool CanManage { get; init; }
}
