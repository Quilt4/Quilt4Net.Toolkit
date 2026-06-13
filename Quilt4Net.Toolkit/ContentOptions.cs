namespace Quilt4Net.Toolkit;

/// <summary>
/// This option can be configured by code or with appsettings.json on location "Quilt4Net/Content"
/// </summary>
public record ContentOptions
{
    /// <summary>
    /// Name of the application that will be used in Quilt4Net.
    /// Default is the name of the assembly.
    /// </summary>
    public string Application { get; set; }

    /// <summary>
    /// Address to the Quilt4Net server.
    /// Default is https://quilt4net.com/. Defaulted on the type so an unbound
    /// <c>IOptions&lt;ContentOptions&gt;</c> still carries a usable URL when only
    /// part of the toolkit is registered (e.g. <c>AddQuilt4NetRemoteConfiguration</c>
    /// without <c>AddQuilt4NetContent</c>).
    /// </summary>
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";

    /// <summary>
    /// Api key to be used for calls to the server.
    /// This key can be retrieved from https://quilt4net.com/.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Duration to cache the stale or default value when an API call fails (e.g. server unreachable, invalid API key).
    /// Only used when no prior successful response TTL is available.
    /// Default is 10 minutes (matching the server's default content TTL).
    /// </summary>
    public TimeSpan FailureCacheDuration { get; set; } = TimeSpan.FromMinutes(10);

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

    /// <summary>
    /// When true (default), the Blazor content registration runs a startup warm-up that pre-fills
    /// the cache with the default language in one bulk call (so pages render without a request per
    /// key). The user's selected language is warmed per-circuit when it differs from the default.
    /// Set false to disable warm-up and rely solely on lazy per-key fetching.
    /// </summary>
    public bool WarmUpEnabled { get; set; } = true;

    /// <summary>
    /// Roles that grant content admin access (edit, debug, reload).
    /// Checked against the authenticated user's claim roles (e.g. Entra ID).
    /// Default is ["ContentAdmin", "Developer"].
    /// </summary>
    public string[] AdminRoles { get; set; } = ["ContentAdmin", "Developer"];

    /// <summary>
    /// When true, always grant content admin access regardless of authentication state.
    /// Useful during development when no identity provider is configured.
    /// Default is false.
    /// </summary>
    public bool AssumeAdmin { get; set; }
}