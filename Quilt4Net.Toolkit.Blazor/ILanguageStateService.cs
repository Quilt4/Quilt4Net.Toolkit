using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

public interface ILanguageStateService
{
    event EventHandler<LanguageLoadedEventArgs> LanguageLoadedEvent;
    event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;
    event EventHandler<DeveloperModeEventArgs> DeveloperModeEvent;
    Language Selected { get; set; }
    Language[] Languages { get; set; }
    bool DeveloperMode { get; set; }
    Task<Language[]> ReloadAsync();
}