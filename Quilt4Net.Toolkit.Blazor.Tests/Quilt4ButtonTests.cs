using Bunit;
using FluentAssertions;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class Quilt4ButtonTests : BunitContext
{
    private readonly FakeContentService _contentService = new();
    private readonly FakeLanguageStateService _languageStateService = new();

    public Quilt4ButtonTests()
    {
        Services.AddSingleton<IContentService>(_contentService);
        Services.AddSingleton<ILanguageStateService>(_languageStateService);
        _languageStateService.Selected = new Language { Key = Guid.NewGuid(), Name = "English" };
    }

    [Fact]
    public void Renders_Text_From_Content_Service()
    {
        _contentService.SetResult("Save Changes");

        var cut = Render<Quilt4Button>(parameters => parameters
            .Add(p => p.TextKey, "btn.save")
            .Add(p => p.DefaultText, "Save"));

        cut.Instance.TextKey.Should().Be("btn.save");
        cut.Find(".rz-button").TextContent.Should().Contain("Save Changes");
    }

    [Fact]
    public void Renders_Default_When_Content_Not_Found()
    {
        _contentService.SetResult(null, success: false);

        var cut = Render<Quilt4Button>(parameters => parameters
            .Add(p => p.TextKey, "btn.missing")
            .Add(p => p.DefaultText, "Fallback Text"));

        cut.Find(".rz-button").TextContent.Should().Contain("Fallback Text");
    }

    [Fact]
    public void Updates_Text_On_Language_Change()
    {
        _contentService.SetResult("Save");

        var cut = Render<Quilt4Button>(parameters => parameters
            .Add(p => p.TextKey, "btn.save")
            .Add(p => p.DefaultText, "Save"));

        cut.Find(".rz-button").TextContent.Should().Contain("Save");

        _contentService.SetResult("Spara");
        _languageStateService.RaiseLanguageChanged();

        cut.WaitForState(() => cut.Find(".rz-button").TextContent.Contains("Spara"));
        cut.Find(".rz-button").TextContent.Should().Contain("Spara");
    }

    private class FakeContentService : IContentService
    {
        private string _value = "";
        private bool _success = true;

        public void SetResult(string value, bool success = true)
        {
            _value = value;
            _success = success;
        }

        public Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType)
        {
            if (_success && !string.IsNullOrEmpty(_value))
                return Task.FromResult((_value, true));
            return Task.FromResult((defaultValue ?? "", _success));
        }

        public Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType) => Task.CompletedTask;
        public Task ClearCacheAsync() => Task.CompletedTask;
    }

    private class FakeLanguageStateService : ILanguageStateService
    {
        public event EventHandler<LanguageLoadedEventArgs> LanguageLoadedEvent;
        public event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;
        public event EventHandler<DeveloperModeEventArgs> DeveloperModeEvent;
        public Language Selected { get; set; }
        public Language[] Languages { get; set; } = [];
        public bool DeveloperMode { get; set; }
        public Task<Language[]> ReloadAsync() => Task.FromResult(Languages);

        public void RaiseLanguageChanged()
        {
            LanguageChangedEvent?.Invoke(this, new LanguageChangedEventArgs());
        }
    }
}
