using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Features.Content;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class ContentWarmUpTests
{
    private const string App = "App1";

    [Fact]
    public async Task WarmCacheAsync_then_GetContentAsync_serves_from_cache_without_a_per_key_call()
    {
        using var listener = StartListener(out var prefix, out var state,
            bulkItems: [("k1", "v1"), ("k2", "v2")]);

        var (warm, content) = Build(prefix);

        await warm.WarmCacheAsync(Guid.Empty, App);
        state.SingleKeyCalls = 0; // ignore anything before the warm; measure reads after it

        var r1 = await content.GetContentAsync("k1", "def", Guid.Empty, ContentFormat.String, App);
        var r2 = await content.GetContentAsync("k2", "def", Guid.Empty, ContentFormat.String, App);

        r1.Should().Be(("v1", true));
        r2.Should().Be(("v2", true));
        state.SingleKeyCalls.Should().Be(0, "warmed keys must be served from cache without a server round-trip");
        state.BulkCalls.Should().Be(1, "warm-up is a single bulk call for the whole application");
    }

    [Fact]
    public async Task WarmCacheAsync_falls_back_to_per_key_when_bulk_endpoint_returns_404()
    {
        using var listener = StartListener(out var prefix, out var state, bulkItems: null, bulkStatus: 404);

        var (warm, content) = Build(prefix);

        await warm.WarmCacheAsync(Guid.Empty, App); // must not throw
        state.SingleKeyCalls = 0;

        var r = await content.GetContentAsync("k1", "def", Guid.Empty, ContentFormat.String, App);

        r.Success.Should().BeTrue();
        r.Value.Should().Be("single-value");
        state.SingleKeyCalls.Should().Be(1, "an old server without the bulk endpoint must leave the per-key path working");
    }

    [Fact]
    public async Task GetCacheCountsByLanguage_reports_loaded_count_per_language()
    {
        using var listener = StartListener(out var prefix, out _, bulkItems: [("k1", "v1"), ("k2", "v2")]);

        var (warm, _) = Build(prefix);
        await warm.WarmCacheAsync(Guid.Empty, App);

        var counts = warm.GetCacheCountsByLanguage();

        counts.Should().ContainKey(Guid.Empty);
        counts[Guid.Empty].Should().Be(2);
    }

    [Fact]
    public async Task WarmCacheAsync_does_nothing_when_no_api_key_configured()
    {
        using var listener = StartListener(out var prefix, out var state, bulkItems: [("k1", "v1")]);

        var (warm, _) = Build(prefix, apiKey: "");

        await warm.WarmCacheAsync(Guid.Empty, App);

        state.BulkCalls.Should().Be(0, "no API key means no calls are made at all");
    }

    private static (IRemoteContentCallService warm, IContentService content) Build(string baseAddress, string apiKey = "test-key")
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddQuilt4NetContent(null, o =>
                {
                    o.Quilt4NetAddress = baseAddress;
                    o.ApiKey = apiKey;
                });
            })
            .Build();

        return (host.Services.GetRequiredService<IRemoteContentCallService>(),
                host.Services.GetRequiredService<IContentService>());
    }

    private sealed class ListenerState
    {
        public int BulkCalls;
        public int SingleKeyCalls;
    }

    private static HttpListener StartListener(out string prefix, out ListenerState state,
        (string Key, string Value)[] bulkItems, int bulkStatus = 200)
    {
        var port = GetFreePort();
        prefix = $"http://127.0.0.1:{port}/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var s = new ListenerState();
        state = s;
        var sync = new object();

        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch { return; }

                var segments = ctx.Request.Url!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var isBulk = segments.Length >= 3
                    && segments[0].Equals("Api", StringComparison.OrdinalIgnoreCase)
                    && segments[1].Equals("Content", StringComparison.OrdinalIgnoreCase)
                    && segments[2].Equals("all", StringComparison.OrdinalIgnoreCase);

                string body;
                if (isBulk)
                {
                    lock (sync) s.BulkCalls++;
                    ctx.Response.StatusCode = bulkStatus;
                    var items = string.Join(",", (bulkItems ?? []).Select(i =>
                        $$"""{"key":"{{i.Key}}","value":"{{i.Value}}"}"""));
                    body = $$"""{"items":[{{items}}],"validTo":"{{DateTime.UtcNow.AddHours(1):o}}"}""";
                }
                else
                {
                    lock (sync) s.SingleKeyCalls++;
                    ctx.Response.StatusCode = 200;
                    body = $$"""{"value":"single-value","validTo":"{{DateTime.UtcNow.AddHours(1):o}}"}""";
                }

                ctx.Response.ContentType = "application/json";
                var buf = System.Text.Encoding.UTF8.GetBytes(body);
                await ctx.Response.OutputStream.WriteAsync(buf);
                ctx.Response.Close();
            }
        });

        return listener;
    }

    private static int GetFreePort()
    {
        using var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
