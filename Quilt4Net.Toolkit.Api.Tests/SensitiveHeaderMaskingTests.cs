using FluentAssertions;
using Quilt4Net.Toolkit.Api;
using Xunit;

namespace Quilt4Net.Toolkit.Api.Tests;

public class SensitiveHeaderMaskingTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("X-API-KEY")]
    [InlineData("Proxy-Authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    public void Default_set_is_masked(string header)
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions());

        compiled.MaskHeaderValue(header, "secret-value").Should().Be(CompiledLoggingOptions.Mask);
    }

    [Fact]
    public void Matching_is_case_insensitive()
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions());

        compiled.MaskHeaderValue("authorization", "Bearer abc").Should().Be(CompiledLoggingOptions.Mask);
        compiled.MaskHeaderValue("x-api-key", "k").Should().Be(CompiledLoggingOptions.Mask);
    }

    [Fact]
    public void Non_sensitive_headers_pass_through_unchanged()
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions());

        compiled.MaskHeaderValue("Content-Type", "application/json").Should().Be("application/json");
        compiled.MaskHeaderValue("X-Correlation-ID", "abc-123").Should().Be("abc-123");
    }

    [Fact]
    public void Opt_out_logs_values_verbatim()
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions { MaskSensitiveHeaders = false });

        compiled.MaskHeaderValue("Authorization", "Bearer abc").Should().Be("Bearer abc");
    }

    [Fact]
    public void Custom_list_replaces_the_default_set()
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions { SensitiveHeaders = ["X-Secret"] });

        // The custom header is masked...
        compiled.MaskHeaderValue("X-Secret", "shh").Should().Be(CompiledLoggingOptions.Mask);
        // ...and a previous default that is no longer listed is NOT masked.
        compiled.MaskHeaderValue("Authorization", "Bearer abc").Should().Be("Bearer abc");
    }

    [Fact]
    public void Empty_sensitive_list_masks_nothing()
    {
        var compiled = new CompiledLoggingOptions(new LoggingOptions { SensitiveHeaders = [] });

        compiled.MaskHeaderValue("Authorization", "Bearer abc").Should().Be("Bearer abc");
    }
}
