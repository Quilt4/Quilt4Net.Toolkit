using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

internal class LanguageStateService : ILanguageStateService
{
    private readonly ILanguageService _languageService;
    private readonly ILocalStorageService _localStorageService;
    private readonly IRemoteContentCallService _remoteContentCallService;
    private Language _selected;
    private bool _developerMode;
    private readonly Language _defaultLanguage;

    public LanguageStateService(ILanguageService languageService, ILocalStorageService localStorageService, IRemoteContentCallService remoteContentCallService)
    {
        _languageService = languageService;
        _localStorageService = localStorageService;
        _remoteContentCallService = remoteContentCallService;
        _defaultLanguage = new Language { Name = null };

        Task.Run(async () =>
        {
            var ls = await ReloadAsync(false);
            try
            {
                var key = await _localStorageService.GetItemAsync<Guid?>("Language.Selected");
                Selected = ls.FirstOrDefault(y => y.Key == key) ?? Selected;
            }
            catch (InvalidOperationException)
            {
                // JS interop not yet available (e.g. static prerender) — keep the default Selected.
            }
            catch (JSDisconnectedException)
            {
                // The Blazor circuit was disposed before this fire-and-forget task got to call JS
                // (browser closed, navigation away, hot reload). Nothing to restore to — drop it.
            }
        });

        _selected = _defaultLanguage;
        Languages = [Selected];
    }

    public event EventHandler<LanguageLoadedEventArgs> LanguageLoadedEvent;
    public event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;
    public event EventHandler<DeveloperModeEventArgs> DeveloperModeEvent;

    public bool DeveloperMode
    {
        get => _developerMode;
        set
        {
            if (_developerMode == value) return;
            _developerMode = value;
            Task.Run(async () =>
            {
                await ReloadAsync(false);
                DeveloperModeEvent?.Invoke(this, new DeveloperModeEventArgs(value));
            });
        }
    }

    public Task<Language[]> ReloadAsync()
    {
        return ReloadAsync(true);
    }

    public async Task<Language[]> ReloadAsync(bool forceReload)
    {
        var languages = await _languageService.GetLanguagesAsync(forceReload);
        if (DeveloperMode)
        {
            languages = languages.Union([
                new Language
                {
                    Name = "X",
                    Key = Language.DeveloperLanguageKey,
                    Developer = true
                }
            ]).ToArray();
        }

        Languages = languages;

        if (Languages.All(x => x.Name != Selected?.Name))
        {
            Selected = Languages.FirstOrDefault() ?? _defaultLanguage;
        }

        LanguageLoadedEvent?.Invoke(this, new LanguageLoadedEventArgs());

        if (languages.All(x => x.Key != Selected.Key))
        {
            Selected = languages.First();
        }
        else
        {
            _selected = languages.Single(x => x.Key == Selected.Key);
        }

        return languages;
    }

    public Language Selected
    {
        get => _selected;
        set
        {
            if (_selected?.Key != value?.Key)
            {
                _selected = value;
                Task.Run(async () =>
                {
                    try
                    {
                        await _localStorageService.SetItemAsync("Language.Selected", _selected.Key);
                    }
                    catch (InvalidOperationException) { /* JS interop unavailable — selection persists for the lifetime of this session only. */ }
                    catch (JSDisconnectedException) { /* Circuit disposed mid-call — nowhere to persist. */ }
                });
                WarmSelected(value);
                LanguageChangedEvent?.Invoke(this, new LanguageChangedEventArgs());
            }
        }
    }

    /// <summary>
    /// Best-effort bulk warm-up of a newly-selected non-default language so subsequent renders in
    /// that language hit cache instead of fanning out per key. Fire-and-forget — the per-key path
    /// still serves the current switch if the warm-up hasn't completed yet. The default language is
    /// warmed at startup by <see cref="ContentWarmupHostedService"/>, so it's skipped here.
    /// </summary>
    private void WarmSelected(Language language)
    {
        var key = language?.Key ?? Guid.Empty;
        if (key == Guid.Empty || key == Language.DeveloperLanguageKey || key == Language.NoApiKeyLanguageKey) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _remoteContentCallService.WarmCacheAsync(key);
            }
            catch
            {
                // Best-effort; the per-key path remains as the fallback.
            }
        });
    }

    public Language[] Languages { get; set; }
}