using Blazored.LocalStorage;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.ApplicationInsights;

internal class ApplicationInsightsConfigurationSelector : IApplicationInsightsConfigurationSelector
{
    private const string StorageKeyPrefix = "Quilt4Net.Monitor.SelectedConfig.";

    /// <summary>
    /// Scope used when the caller doesn't pass one to <see cref="LoadAsync"/>. Without this, every
    /// view that didn't opt in (MetricsView, LogCountByServiceView, VersionMatrixDisplay, plus
    /// LogView when the host omits <c>FilterStorageScope</c>) silently dropped the selection on
    /// every reload. Hosts that need per-team isolation still pass an explicit scope and get a
    /// separate key.
    /// </summary>
    private const string DefaultStorageScope = "default";

    private readonly IApplicationInsightsConfigurationProvider _provider;
    private readonly ILocalStorageService _localStorage;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private string _storageKey;

    public ApplicationInsightsConfigurationResponse Selected { get; private set; }
    public IReadOnlyList<ApplicationInsightsConfigurationResponse> Available { get; private set; } = [];
    public bool IsLoaded { get; private set; }

    public event Action OnChanged;

    public ApplicationInsightsConfigurationSelector(
        IApplicationInsightsConfigurationProvider provider,
        ILocalStorageService localStorage = null)
    {
        _provider = provider;
        _localStorage = localStorage;
    }

    public async Task LoadAsync(string storageScope = null, CancellationToken cancellationToken = default)
    {
        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            if (IsLoaded) return;

            // Fall back to the default scope when the caller doesn't supply one, so the dropdown
            // remembers the operator's choice across page reloads without each view having to opt
            // in. Explicit scopes (LogView's FilterStorageScope, anything host-supplied) still win
            // and keep their per-team / per-tenant isolation.
            var effectiveScope = !string.IsNullOrEmpty(storageScope) ? storageScope : DefaultStorageScope;
            _storageKey = StorageKeyPrefix + effectiveScope;
            Available = await _provider.GetAllAsync(cancellationToken);
            IsLoaded = true;

            if (Available.Count == 0)
            {
                Selected = null;
                OnChanged?.Invoke();
                return;
            }

            string preferredId = null;
            if (_storageKey != null && _localStorage != null)
            {
                try { preferredId = await _localStorage.GetItemAsStringAsync(_storageKey, cancellationToken); }
                catch { /* private mode / quota — fall through to default */ }
            }

            Selected = (preferredId != null ? Available.FirstOrDefault(x => x.Id == preferredId) : null) ?? Available[0];
            OnChanged?.Invoke();
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public async Task SelectAsync(string id)
    {
        var next = Available.FirstOrDefault(x => x.Id == id);
        if (next == null || next.Id == Selected?.Id) return;

        Selected = next;

        if (_storageKey != null && _localStorage != null)
        {
            try { await _localStorage.SetItemAsStringAsync(_storageKey, id); }
            catch { /* localStorage write failures shouldn't break the UI */ }
        }

        OnChanged?.Invoke();
    }
}
