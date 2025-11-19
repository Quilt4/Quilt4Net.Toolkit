using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using Quilt4Net.Toolkit.Features.Api;
using Quilt4Net.Toolkit.Features.Health.Version;
using Xunit;

namespace Quilt4Net.Toolkit.Api.Tests;

public class VersionServiceTests
{
    [Fact]
    public async Task Basic()
    {
        //Arrange
        var environmentName = new Fixture().Create<string>();
        var hostEnvironment = new Mock<IHostEnvironment>(MockBehavior.Strict);
        hostEnvironment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
        var sut = new VersionService(hostEnvironment.Object, new Quilt4NetApiOptions());

        //Act
        var result = await sut.GetVersionAsync(CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.Environment.Should().Be(environmentName);
        result.Machine.Should().Be(Environment.MachineName);
    }
}