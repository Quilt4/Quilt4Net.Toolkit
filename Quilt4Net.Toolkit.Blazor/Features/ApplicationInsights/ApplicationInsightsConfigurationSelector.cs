using Blazored.LocalStorage;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Blazor.Features.ApplicationInsights;

internal class ApplicationInsightsConfigurationSelector : IApplicationInsightsConfigurationSelector
{
    private const string StorageKeyPrefix = "Quilt4Net.Monitor.SelectedConfig.";

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

            _storageKey = !string.IsNullOrEmpty(storageScope) ? StorageKeyPrefix + storageScope : null;
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
