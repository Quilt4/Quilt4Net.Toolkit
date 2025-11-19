namespace Quilt4Net.Toolkit.Features.Api;

public record CertificateCheckOptions
{
    /// <summary>
    /// When using dependency check the feature of certificate checks can be enabled or disabled.
    /// This is a more reliable check since it is done by the caller.
    /// Default is true.
    /// </summary>
    public bool DependencyCheckEnabled { get; set; } = true;

    /// <summary>
    /// The health api will do a self check of the certificate. It will try to find the call information done by the client.
    /// This is not the most reliable since the actual address used could be hidden from the service by routing.
    /// Default is true.
    /// </summary>
    public bool SelfCheckEnabled { get; set; } = true;

    /// <summary>
    /// Specify an explicit uri for the self check.
    /// By Default the request scheme://host will be used.
    /// </summary>
    public string SelfCheckUri { get; set; }

    /// <summary>
    /// When the certificate expiery only have this number of days left, the response will read Degraded.
    /// Default is 30 days.
    /// </summary>
    public int CertExpiryDegradedLimitDays { get; set; } = 30;

    /// <summary>
    /// When the certificate expiery only have this number of days left, the response will read Unhealthy.
    /// Default is 3 days.
    /// </summary>
    public int CertExpiryUnhealthyLimitDays { get; set; } = 3;
}