using Microsoft.JSInterop;

namespace Quilt4Net.Toolkit.Blazor.Framework;

internal sealed class BrowserTimeZoneAccessor : IBrowserTimeZoneAccessor, System.IAsyncDisposable
{
    private const string ModulePath = "./_content/Quilt4Net.Toolkit.Blazor/browser-time-zone.js";

    private readonly IJSRuntime _js;
    private IJSObjectReference _module;
    private System.Threading.Tasks.Task _loadTask;

    public BrowserTimeZoneAccessor(IJSRuntime js)
    {
        _js = js;
        // Default to the server's local timezone before the JS interop completes. On a single-box
        // dev setup the server runs on the same machine as the browser so this is usually correct;
        // in production (server in UTC, browser elsewhere) the JS-interop refinement below
        // replaces it. Anything is better than fixing the initial render to UTC.
        Current = System.TimeZoneInfo.Local;
    }

    public System.TimeZoneInfo Current { get; private set; }
    public bool IsLoaded { get; private set; }

    public event System.Action Changed;

    public System.Threading.Tasks.Task EnsureLoadedAsync()
    {
        // Single-flight: many components on one page call this in OnAfterRenderAsync simultaneously;
        // share the in-flight task so only one JS interop happens per circuit.
        return _loadTask ??= LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            var info = await _module.InvokeAsync<BrowserTimeZoneInfoDto>("getBrowserTimeZone");
            var resolved = Resolve(info);
            if (resolved == null) return;

            Current = resolved;
            IsLoaded = true;
            Changed?.Invoke();
        }
        catch (JSDisconnectedException)
        {
            // Circuit went away between the import dispatch and its reply — nothing to do.
        }
        catch (System.InvalidOperationException)
        {
            // JS interop unavailable (prerender phase). Null the cached task so a later call
            // from OnAfterRenderAsync(firstRender) can retry.
            _loadTask = null;
        }
    }

    /// <summary>
    /// IANA first, offset fallback. The IANA path is DST-aware (a single zone identifier covers
    /// summer + winter); the offset path is what JavaScript gave us *right now* and is
    /// fine-as-a-fallback for chart axis labels. Returns null when the browser sent neither — the
    /// constructor's TimeZoneInfo.Local default then stays in place.
    /// </summary>
    private static System.TimeZoneInfo Resolve(BrowserTimeZoneInfoDto info)
    {
        if (info == null) return null;

        if (!string.IsNullOrWhiteSpace(info.Name))
        {
            try
            {
                return System.TimeZoneInfo.FindSystemTimeZoneById(info.Name);
            }
            catch (System.TimeZoneNotFoundException) { /* fall through to offset */ }
            catch (System.InvalidTimeZoneException) { /* fall through to offset */ }
        }

        var offset = System.TimeSpan.FromMinutes(info.OffsetMinutes);
        if (offset < System.TimeSpan.FromHours(-14) || offset > System.TimeSpan.FromHours(14))
            return null; // bogus payload — keep the existing fallback
        var label = info.Name is { Length: > 0 } ? info.Name : $"UTC{(offset >= System.TimeSpan.Zero ? "+" : "-")}{offset:hh\\:mm}";
        return System.TimeZoneInfo.CreateCustomTimeZone(label, offset, label, label);
    }

    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }

    private sealed record BrowserTimeZoneInfoDto(string Name, int OffsetMinutes);
}
