using FluentAssertions;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Framework;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

/// <summary>
/// Pre-fix repro: a consumer who called <c>AddQuilt4NetRemoteConfiguration()</c> but not
/// <c>AddQuilt4NetContent()</c> got <c>IConnectionService</c> registered (it's part of
/// RemoteConfiguration), but <c>IOptions&lt;ContentOptions&gt;</c> resolved to a default-
/// constructed instance with <c>Quilt4NetAddress = null</c>. The first health probe doing
/// <c>CanConnectAsync(Service.Content)</c> hit <c>new Uri(null)</c> outside the try/catch
/// and crashed with a 23-second elapsed time and "Value cannot be null. (Parameter 'uriString')".
///
/// These tests pin the post-fix shape: option type defaults to https://quilt4net.com/, and
/// any explicit-null sneak path returns a friendly Unhealthy result instead of throwing.
/// </summary>
public class ConnectionServiceUnconfiguredTests
{
    [Fact]
    public void ContentOptions_defaults_Quilt4NetAddress_to_public_url()
    {
        new ContentOptions().Quilt4NetAddress.Should().Be("https://quilt4net.com/");
    }

    [Fact]
    public void RemoteConfigurationOptions_defaults_Quilt4NetAddress_to_public_url()
    {
        new RemoteConfigurationOptions().Quilt4NetAddress.Should().Be("https://quilt4net.com/");
    }

    [Fact]
    public async Task Service_Content_with_null_address_returns_actionable_error_instead_of_throwing()
    {
        var sut = CreateSut(contentAddress: null, configurationAddress: "https://example.com/");

        var result = await sut.CanConnectAsync(Service.Content);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ContentOptions.Quilt4NetAddress")
                     .And.Contain("AddQuilt4NetContent");
    }

    [Fact]
    public async Task Service_Configuration_with_null_address_returns_actionable_error_instead_of_throwing()
    {
        var sut = CreateSut(contentAddress: "https://example.com/", configurationAddress: null);

        var result = await sut.CanConnectAsync(Service.Configuration);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("RemoteConfigurationOptions.Quilt4NetAddress")
                     .And.Contain("AddQuilt4NetRemoteConfiguration");
    }

    [Fact]
    public async Task Malformed_address_returns_friendly_message_with_the_offending_value()
    {
        var sut = CreateSut(contentAddress: "not-a-url", configurationAddress: "https://example.com/");

        var result = await sut.CanConnectAsync(Service.Content);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not-a-url")
                     .And.Contain("absolute URI");
    }

    [Fact]
    public async Task Config_failure_is_cached_so_subsequent_probes_return_immediately()
    {
        var sut = CreateSut(contentAddress: null, configurationAddress: "https://example.com/");

        var first = await sut.CanConnectAsync(Service.Content);
        var second = await sut.CanConnectAsync(Service.Content);

        second.Should().BeSameAs(first);
    }

    private static ConnectionService CreateSut(string contentAddress, string configurationAddress)
    {
        var content = Options.Create(new ContentOptions { Quilt4NetAddress = contentAddress });
        var configuration = Options.Create(new RemoteConfigurationOptions { Quilt4NetAddress = configurationAddress });
        return new ConnectionService(content, configuration);
    }
}
