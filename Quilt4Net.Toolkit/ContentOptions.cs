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
    /// How long to stop calling for a key after a <b>failed</b> call — a timeout, a transport error
    /// or a non-success status other than 404. Doubles per consecutive failure up to
    /// <see cref="MaxFailureCacheDuration"/> and resets on the first success.
    /// Default is 5 seconds.
    /// </summary>
    /// <remarks>
    /// This option previously had no effect for any key that had ever succeeded: the failure path
    /// preferred the last successful response's TTL, which is a content-freshness interval and has
    /// nothing to do with how long a fault should be believed. That is what pinned a value to its
    /// fallback for a full cache lifetime per failed attempt (issue #174), so the preference is
    /// gone and this value now governs the failure hold-off outright.
    /// <para>
    /// A <c>429</c> carrying <c>Retry-After</c> still wins over both this and the back-off — the
    /// server saying when it will be ready beats any local guess.
    /// </para>
    /// </remarks>
    public TimeSpan FailureCacheDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Ceiling for the consecutive-failure back-off described on <see cref="FailureCacheDuration"/>.
    /// A sustained outage settles here instead of retrying every few seconds per key.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan MaxFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long to remember that the server answered <c>404</c> for a key — i.e. there is no content
    /// override and the caller's default stands.
    /// Default is 10 minutes.
    /// </summary>
    /// <remarks>
    /// Deliberately not the failure back-off: a 404 is an <b>answer</b>, not a fault. The server was
    /// reached and replied. Holding it for seconds rather than minutes would re-request every
    /// unseeded key on nearly every render, which is the request flood this feature exists to
    /// remove. Where a previous successful response TTL is known for the key, that is used instead.
    /// </remarks>
    public TimeSpan NotFoundCacheDuration { get; set; } = TimeSpan.FromMinutes(10);

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
    /// Additional languages to warm at startup (and on "Reload Content"), on top of the default
    /// language which is always warmed. Identified by <b>language name</b> exactly as entered on the
    /// server (e.g. <c>["English", "Svenska"]</c>), matching the naming convention used elsewhere in
    /// the content API. A name with no matching server language is skipped with a <c>Warning</c>.
    /// Empty (the default) preserves the previous behaviour — only the default language is warmed at
    /// startup, and other languages warm per-circuit when first selected. Ignored when
    /// <see cref="WarmUpEnabled"/> is false.
    /// </summary>
    public IReadOnlyList<string> WarmUpLanguages { get; set; } = [];

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

    /// <summary>
    /// Diagnostics (issue #132): when a content fetch or language-list load from the server takes
    /// at least this long, a single <c>Warning</c> is logged naming the endpoint, elapsed time and
    /// HTTP status — so slow content/language loads surface in production even when <c>Debug</c>
    /// logging is off. The detailed per-resolution timing lines (key, resolved language, cache
    /// hit/miss, source, elapsed) are logged at <c>Debug</c> on category
    /// <c>Quilt4Net.Toolkit.Features.Content.RemoteContentCallService</c> and are opt-in via normal
    /// log configuration. Set <see cref="TimeSpan.Zero"/> to disable the slow-load warning.
    /// Default is 3 seconds.
    /// </summary>
    public TimeSpan SlowLogThreshold { get; set; } = TimeSpan.FromSeconds(3);
}