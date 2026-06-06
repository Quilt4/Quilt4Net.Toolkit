namespace Quilt4Net.Toolkit.Blazor.Framework;

/// <summary>
/// Holds the current Blazor circuit's browser-side timezone for use in server-rendered displays
/// (charts, grids, audit timestamps, …). The first call to <see cref="EnsureLoadedAsync"/>
/// triggers a JS interop that reads <c>Intl.DateTimeFormat().resolvedOptions().timeZone</c>;
/// subsequent reads of <see cref="Current"/> return the cached value.
/// </summary>
/// <remarks>
/// Lifetime is scoped (per circuit) so prerender and circuit reconnects each get their own
/// resolved value. Before the first successful load <see cref="Current"/> is <c>TimeZoneInfo.Utc</c>
/// — consumers should subscribe to <see cref="Changed"/> and re-render when the real timezone
/// arrives.
/// </remarks>
public interface IBrowserTimeZoneAccessor
{
    /// <summary>The resolved browser timezone, or <c>TimeZoneInfo.Utc</c> until the first load completes.</summary>
    System.TimeZoneInfo Current { get; }

    /// <summary>True once <see cref="EnsureLoadedAsync"/> has completed successfully.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Triggers the JS-interop call on first invocation; idempotent on subsequent calls. Safe to
    /// call from <c>OnAfterRenderAsync(firstRender: true)</c> — JS interop is unavailable during
    /// prerender and any earlier call no-ops.
    /// </summary>
    System.Threading.Tasks.Task EnsureLoadedAsync();

    /// <summary>Fires once after <see cref="EnsureLoadedAsync"/> resolves a non-UTC timezone.</summary>
    event System.Action Changed;
}
