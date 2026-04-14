namespace Quilt4Net.Toolkit;

public record RemoteConfigurationOptions
{
    public TimeSpan? Ttl { get; set; }
    public string Quilt4NetAddress { get; set; }
    public string ApiKey { get; set; }

    /// <summary>
    /// Application name sent to the server when requesting toggle values.
    /// If null (default), the entry assembly name is used.
    /// Set to an empty string to always request shared (cross-application) values.
    /// Set to a specific name to impersonate another application.
    /// </summary>
    public string Application { get; set; }

    /// <summary>
    /// Timeout for HTTP calls to the Quilt4Net server.
    /// Default is 5 seconds. When a stale cached value exists, the caller
    /// gets the stale value immediately and the refresh happens in the background.
    /// This timeout only blocks when no cached value exists.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);
}