using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Xunit;

namespace Quilt4Net.Toolkit.Api.Tests;

public class HealthControllerTests
{
    private readonly Mock<ILiveService> _liveService = new (MockBehavior.Strict);
    private readonly Mock<IReadyService> _readyService = new (MockBehavior.Strict);
    private readonly Mock<IHealthService> _healthService = new (MockBehavior.Strict);
    private readonly Mock<IVersionService> _versionService = new (MockBehavior.Strict);
    private readonly Mock<IMetricsService> _metricsService = new (MockBehavior.Strict);
    private readonly Quilt4NetApiOptions _options = Mock.Of<Quilt4NetApiOptions>();

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Live(string method)
    {
        //Arrange
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        var data = new LiveResponse { Status = LiveStatus.Alive };
        _liveService.Setup(x => x.GetStatusAsync()).ReturnsAsync(data);
        var sub = new HealthController(_liveService.Object, _readyService.Object, _healthService.Object, _versionService.Object, _metricsService.Object, _options)
        {
            HttpContext = httpContext
        };

        //Act
        var response = await sub.Live();

        //Assert
        response.Should().NotBeNull();
        if (method == HttpMethods.Get)
        {
            var result = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, result.StatusCode);

            var payload = Assert.IsType<LiveResponse>(result.Value);
            payload.Status.Should().Be(data.Status);
        }
        else if (method == HttpMethods.Head)
        {
            var result = Assert.IsType<OkResult>(response);
            Assert.Equal(200, result.StatusCode);
        }

        //data
        httpContext.Response.Headers[nameof(LiveResponse.Status)].Single().Should().Be($"{data.Status}");
        _liveService.Verify(x => x.GetStatusAsync(), Times.Once);
    }

    [Theory]
    [InlineData(ReadyStatus.Ready, 200, false)]
    [InlineData(ReadyStatus.Degraded, 200, false)]
    [InlineData(ReadyStatus.Unready, 503, false)]
    [InlineData(ReadyStatus.Ready, 200, true)]
    [InlineData(ReadyStatus.Degraded, 503, true)]
    [InlineData(ReadyStatus.Unready, 503, true)]
    public async Task Ready(ReadyStatus status, int statusCode, bool failReadyWhenDegraded)
    {
        //Arrange
        var options = new Quilt4NetApiOptions
        {
            FailReadyWhenDegraded = failReadyWhenDegraded
        };
        var data = new ReadyResponse { Status = status, Components = [] };
        _readyService.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var sub = new HealthController(_liveService.Object, _readyService.Object, _healthService.Object, _versionService.Object, _metricsService.Object, options);

        //Act
        var response = await sub.Ready(CancellationToken.None);

        //Assert
        response.Should().NotBeNull();
        var result = GetObjectResult(response);
        result.Switch(ok =>
        {
            Assert.Equal(statusCode, ok.StatusCode);
            var payload = Assert.IsType<ReadyResponse>(ok.Value);
            payload.Status.Should().Be(data.Status);
        }, fail => { Assert.Equal(statusCode, fail.StatusCode); });

        _readyService.Verify(x => x.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private OneOf.OneOf<OkObjectResult, ObjectResult> GetObjectResult(IActionResult response)
    {
        if (response.GetType() == typeof(OkObjectResult))
        {
            return Assert.IsType<OkObjectResult>(response);
        }

        return Assert.IsType<ObjectResult>(response);
    }
}