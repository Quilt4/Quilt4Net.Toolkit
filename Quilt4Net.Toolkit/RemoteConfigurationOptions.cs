namespace Quilt4Net.Toolkit;

public record RemoteConfigurationOptions
{
    public TimeSpan? Ttl { get; set; }

    /// <summary>
    /// How long to stop calling for a key after a <b>failed</b> call — a timeout, a transport error
    /// or a non-success status. Doubles per consecutive failure up to
    /// <see cref="MaxFailureCacheDuration"/> and resets on the first success.
    /// Default is 5 seconds.
    /// </summary>
    /// <remarks>
    /// There was no such setting before: configuration hard-coded a private 10-minute constant and
    /// then shadowed even that with the last successful response's TTL, so a toggle whose refreshes
    /// kept timing out was pinned to its fallback for a full lifetime per attempt — two days, in the
    /// case that produced issue #174. A caller reading the toggle could not tell that apart from a
    /// server that genuinely returned the fallback value.
    /// </remarks>
    public TimeSpan FailureCacheDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Ceiling for the consecutive-failure back-off described on <see cref="FailureCacheDuration"/>.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan MaxFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When true (default), configuration resolutions are published as metrics on the
    /// <c>Quilt4Net.Toolkit.Configuration</c> meter — a resolution counter and a duration histogram,
    /// both tagged by source <b>and key</b>, so "which toggle is falling back" is one query.
    /// </summary>
    /// <remarks>
    /// The key is a tag here where it deliberately is not for content: a host has a handful of
    /// toggles, not hundreds of content keys, so the cardinality is bounded and the question is
    /// exactly the one an operator asks.
    /// </remarks>
    public bool MetricsEnabled { get; set; } = true;

    /// <summary>
    /// Address to the Quilt4Net server.
    /// Default is https://quilt4net.com/. Defaulted on the type so an unbound
    /// <c>IOptions&lt;RemoteConfigurationOptions&gt;</c> still carries a usable URL.
    /// </summary>
    public string Quilt4NetAddress { get; set; } = "https://quilt4net.com/";
    public string ApiKey { get; set; }

    /// <summary>
    /// Application name used when a read passes <c>application: null</c> (i.e. "this application").
    /// When <c>null</c> (the default), the entry assembly name is used. Set to an empty string to resolve
    /// such calls to shared (cross-application) values, or to a specific name to impersonate another application.
    /// <para>
    /// Note: the read methods' own <c>application</c> parameter defaults to an empty string (shared), so
    /// this option only takes effect for calls that explicitly pass <c>null</c>.
    /// </para>
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