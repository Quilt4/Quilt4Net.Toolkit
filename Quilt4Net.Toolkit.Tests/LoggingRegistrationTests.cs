using FluentAssertions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class LoggingRegistrationTests
{
    [Fact]
    public void Registers_ITelemetryInitializer()
    {
        var services = new ServiceCollection();
        services.AddQuilt4NetLogging();

        var provider = services.BuildServiceProvider();
        var initializers = provider.GetServices<ITelemetryInitializer>();
        initializers.Should().ContainSingle();
    }

    [Fact]
    public void Callback_can_set_ApplicationName()
    {
        var services = new ServiceCollection();
        services.AddQuilt4NetLogging(options: o => o.ApplicationName = "my-app");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Quilt4NetLoggingOptions>();
        options.ApplicationName.Should().Be("my-app");
    }

    [Fact]
    public void Config_sets_ApplicationName()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Quilt4Net:Logging:ApplicationName"] = "from-config"
            })
            .Build();

        services.AddQuilt4NetLogging(configuration);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Quilt4NetLoggingOptions>();
        options.ApplicationName.Should().Be("from-config");
    }

    [Fact]
    public void Callback_overrides_config()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Quilt4Net:Logging:ApplicationName"] = "from-config"
            })
            .Build();

        services.AddQuilt4NetLogging(configuration, o => o.ApplicationName = "from-callback");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Quilt4NetLoggingOptions>();
        options.ApplicationName.Should().Be("from-callback");
    }

    [Fact]
    public void EnvironmentName_parameter_is_used_as_fallback()
    {
        var services = new ServiceCollection();
        services.AddQuilt4NetLogging(environmentName: "Staging");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Quilt4NetLoggingOptions>();
        options.Environment.Should().Be("Staging");
    }

    [Fact]
    public void Returns_Quilt4NetLoggingBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddQuilt4NetLogging();

        builder.Should().NotBeNull();
        builder.Services.Should().BeSameAs(services);
        builder.Options.Should().NotBeNull();
    }

    [Fact]
    public void Environment_defaults_to_Production_when_nothing_is_configured()
    {
        // Save and clear env vars to test the fallback
        var dotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var aspnet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

            var services = new ServiceCollection();
            services.AddQuilt4NetLogging();

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<Quilt4NetLoggingOptions>();
            options.Environment.Should().Be("Production");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", dotnet);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnet);
        }
    }
}
