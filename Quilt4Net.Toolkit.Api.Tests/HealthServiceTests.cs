using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Features.Health;
using Xunit;

namespace Quilt4Net.Toolkit.Api.Tests;

public class HealthServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProvider = new(MockBehavior.Strict);
    private readonly Mock<Quilt4NetApiOptions> _option = new(MockBehavior.Strict);
    private readonly Mock<ILogger<HealthService>> _logger = new(MockBehavior.Loose);
    private readonly Mock<IHostEnvironment> _hostEnvironment = new(MockBehavior.Strict);

    [Fact]
    public async Task NoComponents()
    {
        //Arrange
        var sut = new HealthService(_hostEnvironment.Object, _serviceProvider.Object, _option.Object, _logger.Object);

        //Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Theory]
    [InlineData(true, true, HealthStatus.Healthy)]
    [InlineData(false, true, HealthStatus.Unhealthy)]
    [InlineData(true, false, HealthStatus.Healthy)]
    [InlineData(false, false, HealthStatus.Degraded)]
    public async Task OneComponent(bool success, bool essential, HealthStatus expectedStatus)
    {
        //Arrange
        var message = new Fixture().Create<string>();
        var component = new Component
        {
            Name = "a",
            Essential = essential,
            CheckAsync = _ => Task.FromResult(new CheckResult { Success = success, Message = message }),
        };
        var option = new Quilt4NetApiOptions();
        option.AddComponent(component);
        var sut = new HealthService(_hostEnvironment.Object, _serviceProvider.Object, option, _logger.Object);

        //Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(expectedStatus);
        result.Components.Single().Key.Should().Be(component.Name);
        result.Components.Single().Value.Status.Should().Be(expectedStatus);
        result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "elapsed").Value.Should().NotBeNull();
        result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "message").Value.Should().Be(message);
    }

    [Theory]
    [InlineData(true, HealthStatus.Unhealthy)]
    [InlineData(false, HealthStatus.Degraded)]
    public async Task ComponentWithException(bool essential, HealthStatus expectedStatus)
    {
        //Arrange
        var component = new Component
        {
            Name = "a",
            Essential = essential,
            CheckAsync = _ => throw new InvalidOperationException("some issue"),
        };
        var option = new Quilt4NetApiOptions();
        option.AddComponent(component);
        _hostEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        var sut = new HealthService(_hostEnvironment.Object, _serviceProvider.Object, option, _logger.Object);

        //Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(expectedStatus);
        result.Components.Single().Key.Should().Be(component.Name);
        result.Components.Single().Value.Status.Should().Be(expectedStatus);
        result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("Hidden exception.");
    }

    [Theory]
    [InlineData(ExceptionDetailLevel.Hidden)]
    [InlineData(ExceptionDetailLevel.Message)]
    [InlineData(ExceptionDetailLevel.StackTrace)]
    public async Task CustomExceptionDataLevel(ExceptionDetailLevel exceptionDetailLevel)
    {
        //Arrange
        var component = new Component
        {
            Name = "a",
            Essential = true,
            CheckAsync = _ => throw new InvalidOperationException("some issue"),
        };
        var option = new Quilt4NetApiOptions();
        option.AddComponent(component);
        option.ExceptionDetail = exceptionDetailLevel;
        var sut = new HealthService(_hostEnvironment.Object, _serviceProvider.Object, option, _logger.Object);

        //Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Components.Single().Key.Should().Be(component.Name);
        result.Components.Single().Value.Status.Should().Be(HealthStatus.Unhealthy);
        switch (exceptionDetailLevel)
        {
            case ExceptionDetailLevel.Hidden:
                result.Components.Single().Value.Details.Count.Should().Be(2);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("Hidden exception.");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().BeNull();
                break;
            case ExceptionDetailLevel.Message:
                result.Components.Single().Value.Details.Count.Should().Be(2);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("some issue");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().BeNull();
                break;
            case ExceptionDetailLevel.StackTrace:
                result.Components.Single().Value.Details.Count.Should().Be(3);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("some issue");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().StartWith("   at");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(exceptionDetailLevel), exceptionDetailLevel, null);
        }
    }

    [Theory]
    [InlineData("Production", ExceptionDetailLevel.Hidden)]
    [InlineData("Development", ExceptionDetailLevel.StackTrace)]
    [InlineData("AllOtherEnvironments", ExceptionDetailLevel.Message)]
    public async Task DefaultExceptionDataLevel(string environment, ExceptionDetailLevel exceptionDetailLevel)
    {
        //Arrange
        var component = new Component
        {
            Name = "a",
            Essential = true,
            CheckAsync = _ => throw new InvalidOperationException("some issue"),
        };
        var option = new Quilt4NetApiOptions();
        option.AddComponent(component);
        _hostEnvironment.Setup(x => x.EnvironmentName).Returns(environment);
        var sut = new HealthService(_hostEnvironment.Object, _serviceProvider.Object, option, _logger.Object);

        //Act
        var result = await sut.GetStatusAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Components.Single().Key.Should().Be(component.Name);
        result.Components.Single().Value.Status.Should().Be(HealthStatus.Unhealthy);
        switch (exceptionDetailLevel)
        {
            case ExceptionDetailLevel.Hidden:
                result.Components.Single().Value.Details.Count.Should().Be(2);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("Hidden exception.");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().BeNull();
                break;
            case ExceptionDetailLevel.Message:
                result.Components.Single().Value.Details.Count.Should().Be(2);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("some issue");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().BeNull();
                break;
            case ExceptionDetailLevel.StackTrace:
                result.Components.Single().Value.Details.Count.Should().Be(3);
                result.Components.Single().Value.Details.First(x => x.Key == "exception.message").Value.Should().StartWith("some issue");
                result.Components.Single().Value.Details.FirstOrDefault(x => x.Key == "exception.stacktrace").Value.Should().StartWith("   at");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(option.ExceptionDetail), option.ExceptionDetail, null);
        }
    }
}