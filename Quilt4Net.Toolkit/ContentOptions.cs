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
    /// Default is https://quilt4net.com/.
    /// </summary>
    public string Quilt4NetAddress { get; set; }

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
    /// Default is 5 seconds. When a stale cached value exists, the caller
    /// gets the stale value immediately and the refresh happens in the background.
    /// This timeout only blocks when no cached value exists.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);

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