namespace Quilt4Net.Toolkit.Blazor.Framework;

/// <summary>
/// Holds the current Blazor circuit's browser-side timezone for use in server-rendered displays
/// (charts, grids, audit timestamps, …). The first call to <see cref="EnsureLoadedAsync"/>
/// triggers a JS interop that reads the browser's IANA timezone and current UTC offset;
/// subsequent reads of <see cref="Current"/> return the cached value. No fixed timezone is ever
/// baked in — the resolved zone always follows whichever machine the browser is running on.
/// </summary>
/// <remarks>
/// Lifetime is scoped (per circuit) so prerender and circuit reconnects each get their own
/// resolved value. Before the JS-interop reply arrives <see cref="Current"/> reports the server's
/// own local timezone (<c>TimeZoneInfo.Local</c>) — on a single-machine dev setup that's already
/// browser-local, and in any case it gets replaced once the interop completes. Consumers that
/// want to refresh their UI when the real browser timezone lands should subscribe to <see cref="Changed"/>.
/// </remarks>
public interface IBrowserTimeZoneAccessor
{
    /// <summary>
    /// The resolved browser timezone. Starts at <see cref="System.TimeZoneInfo.Local"/> (the
    /// server's own zone) and switches to the browser's actual zone once the JS interop replies.
    /// </summary>
    System.TimeZoneInfo Current { get; }

    /// <summary>True once <see cref="EnsureLoadedAsync"/> has resolved the browser's timezone.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Triggers the JS-interop call on first invocation; idempotent on subsequent calls. Safe to
    /// call from <c>OnAfterRenderAsync(firstRender: true)</c> — JS interop is unavailable during
    /// prerender and any earlier call is allowed to retry.
    /// </summary>
    System.Threading.Tasks.Task EnsureLoadedAsync();

    /// <summary>Fires once after <see cref="EnsureLoadedAsync"/> resolves the browser's timezone.</summary>
    event System.Action Changed;
}
