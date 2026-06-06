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
        Current = System.TimeZoneInfo.Utc;
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
            var ianaName = await _module.InvokeAsync<string>("getBrowserTimeZone");
            if (string.IsNullOrWhiteSpace(ianaName)) return;

            // .NET 10 maps IANA ids natively on all platforms via ICU; TryFindSystemTimeZoneById
            // would be cleaner but isn't on the surface. Catch + log-and-skip on the rare
            // exotic id rather than crash the circuit.
            try
            {
                Current = System.TimeZoneInfo.FindSystemTimeZoneById(ianaName);
            }
            catch (System.TimeZoneNotFoundException)
            {
                return;
            }
            catch (System.InvalidTimeZoneException)
            {
                return;
            }

            IsLoaded = true;
            Changed?.Invoke();
        }
        catch (JSDisconnectedException)
        {
            // Circuit went away between the import dispatch and its reply — nothing to do.
        }
        catch (System.InvalidOperationException)
        {
            // JS interop unavailable (prerender phase). The next OnAfterRenderAsync call will
            // re-run this — null out the cached task so the retry is allowed.
            _loadTask = null;
        }
    }

    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }
}
