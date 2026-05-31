namespace Quilt4Net.Toolkit;

public record RemoteConfigurationOptions
{
    public TimeSpan? Ttl { get; set; }

    /// <summary>
    /// Address to the Quilt4Net server.
    /// Default is https://quilt4net.com/. Defaulted on the type so an unbound
    /// <c>IOptions&lt;RemoteConfigurationOptions&gt;</c> still carries a usable URL.
    /// </summary>
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";
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
    /// Default is 5 seconds. When a stale cached value exists and <see cref="StaleWhileRevalidate"/>
    /// is enabled, the caller gets the stale value immediately and the refresh happens in the
    /// background, so this timeout only blocks when no cached value exists. When
    /// <see cref="StaleWhileRevalidate"/> is disabled, an expired entry is refreshed synchronously
    /// and this timeout applies to that call.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When true (default), an expired cache entry is returned immediately and refreshed in the
    /// background (stale-while-revalidate) — fast, but the caller may see one slightly stale value.
    /// When false, an expired entry is refreshed synchronously so the caller always gets a fresh
    /// value (subject to <see cref="HttpTimeout"/>), at the cost of blocking on the refresh.
    /// </summary>
    public bool StaleWhileRevalidate { get; set; } = true;
}