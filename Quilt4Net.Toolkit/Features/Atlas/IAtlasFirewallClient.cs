namespace Quilt4Net.Toolkit.Features.Atlas;

/// <summary>
/// Typed client over Quilt4Net's firewall proxy endpoints for a single Atlas project, bound to one
/// <c>AtlasFirewallProxyKeyEntry</c> (from a value-group bundle). Quilt4Net performs the Atlas change;
/// the caller never holds an Atlas credential. Create via <see cref="IAtlasFirewallClientFactory"/>.
/// </summary>
public interface IAtlasFirewallClient
{
    /// <summary>Open <paramref name="ip"/> (IP or CIDR) on the project's access list. Requires a manage key. Idempotent.</summary>
    Task<FirewallOpenResult> OpenAsync(string ip, string name = null, CancellationToken cancellationToken = default);

    /// <summary>Close (remove) <paramref name="ip"/> from the access list. Requires a manage key.</summary>
    Task<FirewallCloseResult> CloseAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>Report that <paramref name="ip"/> is in use so Quilt4Net defers auto-closing it (heartbeat). Works with any firewall key.</summary>
    Task<FirewallUsageResult> ReportUsedAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>Current state of <paramref name="ip"/>, or null if it is neither tracked nor open. Works with any firewall key.</summary>
    Task<FirewallStateResult> GetStateAsync(string ip, CancellationToken cancellationToken = default);
}

/// <summary>Result of <see cref="IAtlasFirewallClient.OpenAsync"/>. <see cref="Outcome"/>: Opened | AlreadyOpen | Failed.</summary>
public record FirewallOpenResult
{
    public string Outcome { get; init; }
    public string Ip { get; init; }
    public DateTime? OpenedUtc { get; init; }
    public DateTime? LastUsedUtc { get; init; }
    public DateTime? ClosesAtUtc { get; init; }
}

/// <summary>Result of <see cref="IAtlasFirewallClient.CloseAsync"/>. <see cref="Outcome"/>: Closed | NotOpen | Failed.</summary>
public record FirewallCloseResult
{
    public string Outcome { get; init; }
}

/// <summary>Result of <see cref="IAtlasFirewallClient.ReportUsedAsync"/>. <see cref="Outcome"/>: Recorded | RecordedNoCredential.</summary>
public record FirewallUsageResult
{
    public string Outcome { get; init; }
    public DateTime? LastUsedUtc { get; init; }
    public DateTime? ClosesAtUtc { get; init; }
}

/// <summary>Result of <see cref="IAtlasFirewallClient.GetStateAsync"/>.</summary>
public record FirewallStateResult
{
    public string Ip { get; init; }
    public bool IsOpen { get; init; }
    public DateTime? OpenedUtc { get; init; }
    public DateTime? LastUsedUtc { get; init; }
    public DateTime? ClosesAtUtc { get; init; }
    public bool IsPermanent { get; init; }
}
