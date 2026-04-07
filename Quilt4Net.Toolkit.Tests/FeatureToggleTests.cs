using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    private static int GetFreePort()
    {
        using var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
