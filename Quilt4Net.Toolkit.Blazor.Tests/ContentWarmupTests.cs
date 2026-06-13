using System.Collections.Concurrent;
using Blazored.LocalStorage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Xunit;

namespace Quilt4Net.Toolkit.Blazor.Tests;

public class ContentWarmupTests
{
    [Fact]
    public async Task HostedService_warms_the_default_language_on_start_when_enabled()
    {
        var call = new RecordingRemoteCallService();
        var sut = new ContentWarmupHostedService(call, Options.Create(new ContentOptions { WarmUpEnabled = true }),
            NullLogger<ContentWarmupHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        (await WaitUntil(() => call.Calls.Any(c => c == Guid.Empty))).Should().BeTrue();
        call.Calls.Should().ContainSingle().Which.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task HostedService_does_not_warm_when_disabled()
    {
        var call = new RecordingRemoteCallService();
        var sut = new ContentWarmupHostedService(call, Options.Create(new ContentOptions { WarmUpEnabled = false }),
            NullLogger<ContentWarmupHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await Task.Delay(150);
        call.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task LanguageStateService_warms_a_newly_selected_non_default_language()
    {
        var call = new RecordingRemoteCallService();
        var other = new Language { Key = Guid.NewGuid(), Name = "Swedish" };
        var languages = new[] { new Language { Key = Guid.Empty, Name = "Default" }, other };
        var sut = new LanguageStateService(new FakeLanguageService(languages), new NoopLocalStorage(), call);

        sut.Selected = other;

        (await WaitUntil(() => call.Calls.Contains(other.Key))).Should().BeTrue("selecting a non-default language warms its content");
        call.Calls.Should().NotContain(Guid.Empty, "the default language is warmed at startup, not by the selector");
    }

    private static async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (condition()) return true;
            await Task.Delay(20);
        }
        return condition();
    }

    private sealed class RecordingRemoteCallService : IRemoteContentCallService
    {
        public ConcurrentBag<Guid> Calls { get; } = [];

        public Task WarmCacheAsync(Guid languageKey, string application = null)
        {
            Calls.Add(languageKey);
            return Task.CompletedTask;
        }

        public Task<(string Value, bool Success)> GetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat? contentType, string application = null)
            => Task.FromResult((defaultValue, true));
        public Task SetContentAsync(string key, string defaultValue, Guid languageKey, ContentFormat contentType, string application = null) => Task.CompletedTask;
        public Task<Language[]> GetLanguagesAsync(bool forceReload) => Task.FromResult(Array.Empty<Language>());
        public Task ClearContentCacheAsync() => Task.CompletedTask;
        public IReadOnlyDictionary<Guid, int> GetCacheCountsByLanguage() => new Dictionary<Guid, int>();
    }

    private sealed class FakeLanguageService(Language[] languages) : ILanguageService
    {
        public Task<Language[]> GetLanguagesAsync(bool forceReload) => Task.FromResult(languages);
    }

    // Only the two members LanguageStateService touches are implemented; the rest are unused here.
    private sealed class NoopLocalStorage : ILocalStorageService
    {
        public ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default) => new(default(T));
        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask ClearAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<string> KeyAsync(int index, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default) => throw new NotImplementedException();

#pragma warning disable CS0067
        public event EventHandler<ChangingEventArgs> Changing;
        public event EventHandler<ChangedEventArgs> Changed;
#pragma warning restore CS0067
    }
}
