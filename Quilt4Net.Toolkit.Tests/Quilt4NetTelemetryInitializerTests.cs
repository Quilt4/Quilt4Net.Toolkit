using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Quilt4Net.Toolkit.Features.Logging;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class Quilt4NetTelemetryInitializerTests
{
    [Fact]
    public void Sets_RoleName_from_ApplicationName()
    {
        var options = new Quilt4NetLoggingOptions { ApplicationName = "my-app" };
        var initializer = new Quilt4NetTelemetryInitializer(options);
        var telemetry = new TraceTelemetry("test");

        initializer.Initialize(telemetry);

        telemetry.Context.Cloud.RoleName.Should().Be("my-app");
    }

    [Fact]
    public void Sets_Version_from_options()
    {
        var options = new Quilt4NetLoggingOptions { Version = "1.2.3" };
        var initializer = new Quilt4NetTelemetryInitializer(options);
        var telemetry = new TraceTelemetry("test");

        initializer.Initialize(telemetry);

        telemetry.Context.Component.Version.Should().Be("1.2.3");
    }

    [Fact]
    public void Sets_Environment_in_GlobalProperties()
    {
        var options = new Quilt4NetLoggingOptions { Environment = "Staging" };
        var initializer = new Quilt4NetTelemetryInitializer(options);
        var telemetry = new TraceTelemetry("test");

        initializer.Initialize(telemetry);

        telemetry.Context.GlobalProperties.Should().ContainKey("Environment")
            .WhoseValue.Should().Be("Staging");
    }

    [Fact]
    public void Does_not_set_properties_when_values_are_null()
    {
        var options = new Quilt4NetLoggingOptions();
        var initializer = new Quilt4NetTelemetryInitializer(options);
        var telemetry = new TraceTelemetry("test");

        initializer.Initialize(telemetry);

        telemetry.Context.Cloud.RoleName.Should().BeNullOrEmpty();
        telemetry.Context.Component.Version.Should().BeNullOrEmpty();
        telemetry.Context.GlobalProperties.Should().NotContainKey("Environment");
    }

    [Fact]
    public void Sets_all_properties_together()
    {
        var options = new Quilt4NetLoggingOptions
        {
            ApplicationName = "florida-server",
            Version = "2.0.0",
            Environment = "Production"
        };
        var initializer = new Quilt4NetTelemetryInitializer(options);
        var telemetry = new ExceptionTelemetry(new Exception("test"));

        initializer.Initialize(telemetry);

        telemetry.Context.Cloud.RoleName.Should().Be("florida-server");
        telemetry.Context.Component.Version.Should().Be("2.0.0");
        telemetry.Context.GlobalProperties["Environment"].Should().Be("Production");
    }
}
