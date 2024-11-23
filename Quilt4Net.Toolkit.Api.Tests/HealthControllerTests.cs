using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Quilt4Net.Toolkit.Api.Features.Health;
using Quilt4Net.Toolkit.Api.Features.Live;
using Quilt4Net.Toolkit.Api.Features.Metrics;
using Quilt4Net.Toolkit.Api.Features.Ready;
using Quilt4Net.Toolkit.Api.Features.Version;
using Quilt4Net.Toolkit.Features.Health;
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
        var sut = new HealthController(_liveService.Object, _readyService.Object, _healthService.Object, _versionService.Object, _metricsService.Object, _options)
        {
            HttpContext = httpContext
        };

        //Act
        var response = await sut.Live();

        //Assert
        response.Should().NotBeNull();
        AssertStatusResponse<LiveResponse, LiveStatus>(method, response, data.Status, 200);
        httpContext.Response.Headers[nameof(LiveResponse.Status)].Single().Should().Be($"{data.Status}");
        _liveService.Verify(x => x.GetStatusAsync(), Times.Once);
    }

    //TODO: Build a method to generate this
    [Theory]
    [InlineData(ReadyStatus.Ready, 200, false, "GET")]
    [InlineData(ReadyStatus.Degraded, 200, false, "GET")]
    [InlineData(ReadyStatus.Unready, 503, false, "GET")]
    [InlineData(ReadyStatus.Ready, 200, true, "GET")]
    [InlineData(ReadyStatus.Degraded, 503, true, "GET")]
    [InlineData(ReadyStatus.Unready, 503, true, "GET")]
    [InlineData(ReadyStatus.Ready, 200, false, "HEAD")]
    [InlineData(ReadyStatus.Degraded, 200, false, "HEAD")]
    [InlineData(ReadyStatus.Unready, 503, false, "HEAD")]
    [InlineData(ReadyStatus.Ready, 200, true, "HEAD")]
    [InlineData(ReadyStatus.Degraded, 503, true, "HEAD")]
    [InlineData(ReadyStatus.Unready, 503, true, "HEAD")]
    public async Task Ready(ReadyStatus status, int statusCode, bool failReadyWhenDegraded, string method)
    {
        //Arrange
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        var options = new Quilt4NetApiOptions { FailReadyWhenDegraded = failReadyWhenDegraded };
        var data = new ReadyResponse { Status = status, Components = [] };
        _readyService.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var sut = new HealthController(_liveService.Object, _readyService.Object, _healthService.Object, _versionService.Object, _metricsService.Object, options)
        {
            HttpContext = httpContext
        };

        //Act
        var response = await sut.Ready(CancellationToken.None);

        //Assert
        response.Should().NotBeNull();
        AssertStatusResponse<ReadyResponse, ReadyStatus>(method, response, data.Status, statusCode);
        httpContext.Response.Headers[nameof(ReadyResponse.Status)].Single().Should().Be($"{data.Status}");
        _readyService.Verify(x => x.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, 200, "GET")]
    [InlineData(HealthStatus.Degraded, 200, "GET")]
    [InlineData(HealthStatus.Unhealthy, 503, "GET")]
    [InlineData(HealthStatus.Healthy, 200, "HEAD")]
    [InlineData(HealthStatus.Degraded, 200, "HEAD")]
    [InlineData(HealthStatus.Unhealthy, 503, "HEAD")]
    public async Task Health(HealthStatus status, int statusCode, string method)
    {
        //Arrange
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        var options = new Quilt4NetApiOptions();
        var data = new HealthResponse { Status = status, Components = [] };
        _healthService.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var sut = new HealthController(_liveService.Object, _readyService.Object, _healthService.Object, _versionService.Object, _metricsService.Object, options)
        {
            HttpContext = httpContext
        };

        //Act
        var response = await sut.Health(CancellationToken.None);

        //Assert
        response.Should().NotBeNull();
        AssertStatusResponse<HealthResponse, HealthStatus>(method, response, data.Status, statusCode);
        httpContext.Response.Headers[nameof(ReadyResponse.Status)].Single().Should().Be($"{data.Status}");
        _healthService.Verify(x => x.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void AssertStatusResponse<TResponse, TStatus>(string method, IActionResult response, TStatus status, int statusCode)
            where TResponse : ResponseBase<TStatus>
            where TStatus : Enum
    {
        switch (method)
        {
            case "GET":
            {
                var result = GetResult(response);
                result.Switch(okObjectResult =>
                {
                    okObjectResult.StatusCode.Should().Be(statusCode);
                    var payload = Assert.IsType<TResponse>(okObjectResult.Value);
                    payload.Status.Should().Be(status);
                }, objectResult =>
                {
                    objectResult.StatusCode.Should().Be(statusCode);
                });
                break;
            }
            case "HEAD":
            {
                var result = HeadResult(response);
                result.Switch(okResult =>
                {
                    okResult.StatusCode.Should().Be(statusCode);
                }, statusCodeResult =>
                {
                    statusCodeResult.StatusCode.Should().Be(statusCode);
                });
                break;
            }
        }
    }

    private static OneOf.OneOf<OkObjectResult, ObjectResult> GetResult(IActionResult response)
    {
        if (response.GetType() == typeof(OkObjectResult))
        {
            return Assert.IsType<OkObjectResult>(response);
        }

        return Assert.IsType<ObjectResult>(response);
    }

    private static OneOf.OneOf<OkResult, StatusCodeResult> HeadResult(IActionResult response)
    {
        if (response.GetType() == typeof(OkResult))
        {
            return (OkResult)response;
        }

        return (StatusCodeResult)response;
    }
}