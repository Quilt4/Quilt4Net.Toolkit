using Quilt4Net.Toolkit.Features.Content;

namespace Quilt4Net.Toolkit.Blazor;

public class LanguageChangedEventArgs : EventArgs
{
}

public interface ILanguageStateService
{
    event EventHandler<LanguageChangedEventArgs>  LanguageChangedEvent;
    Language Selected { get; set; }
    Language[] Languages { get; set; }
}

internal class LanguageStateService : ILanguageStateService
{
    private Language _selected;

    public LanguageStateService(ILanguageService languageService)
    {
        var d = new Language { Name = null };

        Task.Run(async () =>
        {
            Languages = await languageService.GetLanguagesAsync();
            if (Languages.All(x => x.Name != Selected?.Name))
            {
                Selected = Languages.FirstOrDefault() ?? d;
            }
        });

        //TODO: Pick from cookie
        //TODO: If nothing, select "default"
        Selected = d;
        Languages = [Selected];
    }

    public event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;

    public Language Selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                LanguageChangedEvent?.Invoke(this, new LanguageChangedEventArgs());
            }
        }
    }

    public Language[] Languages { get; set; }
}

public interface IEditContentService
{
    event EventHandler<EditModeEventArgs> EditModeEvent;
    bool Enabled { get; set; }
}