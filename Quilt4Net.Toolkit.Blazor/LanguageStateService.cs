using Blazored.LocalStorage;
using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

internal class LanguageStateService : ILanguageStateService
{
    private readonly ILanguageService _languageService;
    private readonly ILocalStorageService _localStorageService;
    private Language _selected;
    private bool _developerMode;
    private readonly Language _defaultLanguage;

    public LanguageStateService(ILanguageService languageService, ILocalStorageService localStorageService)
    {
        _languageService = languageService;
        _localStorageService = localStorageService;
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
                // ignored
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
                    Key = Guid.Parse("8C12E829-318E-40DA-86E9-6B37A68EFFD1"),
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
                    await _localStorageService.SetItemAsync("Language.Selected", _selected.Key);
                });
                LanguageChangedEvent?.Invoke(this, new LanguageChangedEventArgs());
            }
        }
    }

    public Language[] Languages { get; set; }
}