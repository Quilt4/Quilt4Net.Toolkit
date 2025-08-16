using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.Measure;
using System.Text;
using Quilt4Net.Toolkit.Api.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class DataSampleController : ControllerBase
{
    private readonly IFeatureToggleService _featureToggleService;
    private readonly ILogger<DataSampleController> _logger;

    public DataSampleController(IFeatureToggleService featureToggleService, ILogger<DataSampleController> logger)
    {
        _featureToggleService = featureToggleService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostPayload([FromHeader] string header, [FromQuery] string query, [FromBody] SampleData data)
    {
        return await _logger.MeasureAsync(nameof(PostPayload), async d =>
        {
            d.AddField("Something", "yeee");

            HttpContext.Response.Headers.Add("Method", nameof(PostPayload));
            return Ok(new { header, query, data });
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPayload([FromHeader] string header, [FromQuery] string query)
    {
        var toggle = await _featureToggleService.GetToggleAsync("MyBool", ttl: TimeSpan.FromSeconds(10));

        if (toggle) return Unauthorized();

        var intValue = await _featureToggleService.GetValueAsync("MyInt", 42);
        var stringValue = await _featureToggleService.GetValueAsync("MyString", "yeee");
        var myDateValue = await _featureToggleService.GetValueAsync("MyDate", DateTime.UtcNow);
        var myTimeSpanValue = await _featureToggleService.GetValueAsync("MyTimeSpan", TimeSpan.FromSeconds(10));
        var myDecimalValue = await _featureToggleService.GetValueAsync("MyDecimal", 1.2M);
        var mySingleValue = await _featureToggleService.GetValueAsync("MySingle", 1.2F);
        var myNBool = await _featureToggleService.GetValueAsync<bool?>("MySingle", false);
        var mySampleData = await _featureToggleService.GetValueAsync("MySampleData", new SampleData { SomeDate = myDateValue, SomeInt = 123 });

        _logger.LogInformation("{Method} {Function} called with header {Header} and query {Query}.", "HttpGet", nameof(GetPayload), header, query);
        var data = new SampleData { SomeInt = intValue, SomeDate = myDateValue };
        HttpContext.Response.Headers.Add("Method", nameof(GetPayload));
        return Ok(new { header, query, data });
    }

    [HttpGet("stream")]
    public async Task Stream()
    {
        Response.ContentType = "text/plain";
        if (HttpContext.Response.Headers.ContainsKey("X-Accel-Buffering")) HttpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var stream = Response.Body;
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        for (var i = 0; i < 10; i++)
        {
            await writer.WriteLineAsync($"Line {i}");
            await writer.FlushAsync();
            await Task.Delay(1000);
        }

        await writer.WriteLineAsync("Done");
        await writer.FlushAsync();
    }
}