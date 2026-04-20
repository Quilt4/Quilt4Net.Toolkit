using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Quilt4Net.Toolkit.Framework;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class FeatureToggleTests
{
    [Fact]
    public async Task GetToggleAsync_Returns_Fallback_When_Server_Returns_401()
    {
        // Arrange — start a local HTTP listener that always returns 401
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var listenerTask = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            ctx.Response.StatusCode = 401;
            ctx.Response.Close();
        });

        try
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddQuilt4NetRemoteConfiguration(null, o =>
                    {
                        o.Quilt4NetAddress = prefix;
                        o.ApiKey = "test-key";
                    });
                })
                .Build();

            var service = host.Services.GetRequiredService<IFeatureToggleService>();

            // Act & Assert — should NOT throw, should return fallback
            var result = await service.GetToggleAsync("my-toggle", fallback: true);

            result.Should().BeTrue("fallback value should be returned when server returns 401");
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task GetToggleAsync_Returns_Fallback_When_Server_Unreachable()
    {
        // Arrange — point at an address where nothing is listening
        var port = GetFreePort();
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddQuilt4NetRemoteConfiguration(null, o =>
                {
                    o.Quilt4NetAddress = $"http://127.0.0.1:{port}/";
                    o.ApiKey = "test-key";
                });
            })
            .Build();

        var service = host.Services.GetRequiredService<IFeatureToggleService>();

        // Act & Assert — should NOT throw, should return fallback
        var result = await service.GetToggleAsync("my-toggle", fallback: true);

        result.Should().BeTrue("fallback value should be returned when server is unreachable");
    }

    [Fact]
    public async Task GetToggleAsync_Returns_Fallback_When_Server_Returns_500()
    {
        // Arrange — start a local HTTP listener that returns 500
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var listenerTask = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            ctx.Response.StatusCode = 500;
            ctx.Response.Close();
        });

        try
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddQuilt4NetRemoteConfiguration(null, o =>
                    {
                        o.Quilt4NetAddress = prefix;
                        o.ApiKey = "test-key";
                    });
                })
                .Build();

            var service = host.Services.GetRequiredService<IFeatureToggleService>();

            // Act & Assert
            var result = await service.GetToggleAsync("my-toggle", fallback: false);

            result.Should().BeFalse("fallback value should be returned when server returns 500");
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task GetToggleAsync_Same_Key_Different_Application_Are_Separate_Cache_Entries()
    {
        // Arrange — local listener that counts requests and returns the request's
        // base64-encoded complex key in the body so we can verify the second call
        // actually hit the server (different application = different cache entry).
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var hits = 0;
        var receivedApplications = new System.Collections.Concurrent.ConcurrentBag<string>();
        var listenerTask = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch { return; }
                Interlocked.Increment(ref hits);

                // URL is /Api/Configuration/{base64(complexKey)} — decode the request to capture which Application was sent.
                var segments = ctx.Request.Url!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 3)
                {
                    var encoded = WebUtility.UrlDecode(segments[2]);
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Application", out var appProp))
                        receivedApplications.Add(appProp.GetString());
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                var body = $$"""{"value":"True","validTo":"{{DateTime.UtcNow.AddHours(1):o}}"}""";
                var buf = System.Text.Encoding.UTF8.GetBytes(body);
                await ctx.Response.OutputStream.WriteAsync(buf);
                ctx.Response.Close();
            }
        });

        try
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddQuilt4NetRemoteConfiguration(null, o =>
                    {
                        o.Quilt4NetAddress = prefix;
                        o.ApiKey = "test-key";
                    });
                })
                .Build();

            var service = host.Services.GetRequiredService<IRemoteConfigurationService>();

            // Act — same key, two different applications
            await service.GetAsync("my-toggle", false, application: "App1");
            await service.GetAsync("my-toggle", false, application: "App2");

            // Assert — both calls must hit the server because the cache key
            // includes the effective application. With a key-only cache, the
            // second call would silently return the first call's cached value.
            hits.Should().Be(2,
                "same toggle key with different application is a different cache entry — neither call may use the other's cached value");
            receivedApplications.Should().BeEquivalentTo(["App1", "App2"]);
        }
        finally
        {
            listener.Stop();
        }
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
