using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Radzen;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

// Issue #141: the commonly-used components accept an optional name-keyed Translations dictionary and
// forward it to the content service (which sends it on the wire; the server applies it only on first
// creation). These tests assert the forwarding for both integration paths (direct GetContentAsync
// callers and PlaceholderResolver callers) and the split-button item model.
public class ComponentTranslationsTests : BunitContext
{
    private readonly CapturingContentService _content = new();

    public ComponentTranslationsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IContentService>(_content);
        Services.AddSingleton<IEditContentService>(new StubEditContentService());
        Services.AddSingleton<IContentSourceService>(new ContentSourceService());
        Services.AddSingleton<ILanguageStateService>(new StubLanguageState());
        Services.AddScoped<DialogService>();
    }

    [Fact]
    public void Quilt4Text_forwards_Translations_to_the_content_service()
    {
        var translations = new Dictionary<string, string> { ["Swedish"] = "Ärende" };

        Render<Quilt4Text>(p => p
            .Add(x => x.Key, "case.subject")
            .Add(x => x.Default, "Case subject")
            .Add(x => x.Translations, translations));

        _content.TranslationsSeen.Should().Contain(translations);
    }

    [Fact]
    public void Quilt4Text_without_Translations_forwards_null()
    {
        Render<Quilt4Text>(p => p
            .Add(x => x.Key, "case.subject")
            .Add(x => x.Default, "Case subject"));

        _content.TranslationsSeen.Should().OnlyContain(t => t == null);
    }

    [Fact]
    public void Quilt4RadzenPanelMenuItem_forwards_Translations()
    {
        var translations = new Dictionary<string, string> { ["Swedish"] = "Hem" };

        Render<Quilt4RadzenPanelMenuItem>(p => p
            .Add(x => x.TextKey, "menu.home")
            .Add(x => x.DefaultText, "Home")
            .Add(x => x.Translations, translations));

        _content.TranslationsSeen.Should().Contain(translations);
    }

    [Fact]
    public void Quilt4Tooltip_forwards_Translations_via_the_resolver()
    {
        var translations = new Dictionary<string, string> { ["Swedish"] = "Ta bort raden" };

        Render<Quilt4Tooltip>(p => p
            .Add(x => x.TooltipKey, "tip.delete")
            .Add(x => x.DefaultTooltip, "Delete this row")
            .Add(x => x.Translations, translations));

        _content.TranslationsSeen.Should().Contain(translations);
    }

    [Fact]
    public void Quilt4SplitButton_forwards_per_item_Translations()
    {
        var itemTranslations = new Dictionary<string, string> { ["Swedish"] = "Ta bort" };

        Render<Quilt4SplitButton>(p => p
            .Add(x => x.TextKey, "row.open")
            .Add(x => x.DefaultText, "Open")
            .Add(x => x.Items, new List<Quilt4SplitButtonItem>
            {
                new() { TextKey = "row.delete", DefaultText = "Delete", Value = "delete", Translations = itemTranslations },
            }));

        _content.TranslationsSeen.Should().Contain(itemTranslations);
    }

    private sealed class CapturingContentService : IContentService
    {
        public List<IReadOnlyDictionary<string, string>> TranslationsSeen { get; } = new();

        public Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null, IReadOnlyDictionary<string, string> translations = null)
        {
            TranslationsSeen.Add(translations);
            return Task.FromResult((defaultValue ?? "", false));
        }
        public Task SetContentAsync(string key, string value, Guid languageKey, ContentFormat contentType, string application = null) => Task.CompletedTask;
        public Task ClearCacheAsync() => Task.CompletedTask;
    }

    private sealed class StubEditContentService : IEditContentService
    {
        public event EventHandler<EditModeEventArgs> EditModeEvent;
        public bool Enabled { get; set; }
        private void Unused() => EditModeEvent?.Invoke(this, null);
    }

    private sealed class StubLanguageState : ILanguageStateService
    {
        public event EventHandler<LanguageLoadedEventArgs> LanguageLoadedEvent;
        public event EventHandler<LanguageChangedEventArgs> LanguageChangedEvent;
        public event EventHandler<DeveloperModeEventArgs> DeveloperModeEvent;
        public Language Selected { get; set; } = new() { Name = "Swedish", Key = Guid.NewGuid() };
        public Language[] Languages { get; set; } = [];
        public bool DeveloperMode { get; set; }
        public Task<Language[]> ReloadAsync() => Task.FromResult(Languages);
        private void Unused() { LanguageLoadedEvent?.Invoke(this, null); LanguageChangedEvent?.Invoke(this, null); DeveloperModeEvent?.Invoke(this, null); }
    }
}
