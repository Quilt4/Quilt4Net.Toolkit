using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.Measure;
using System.Text;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class DataSampleController : ControllerBase
{
    private readonly ILogger<DataSampleController> _logger;

    public DataSampleController(ILogger<DataSampleController> logger)
    {
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
    public Task<IActionResult> GetPayload([FromHeader] string header, [FromQuery] string query)
    {
        _logger.LogInformation("{Method} {Function} called with header {Header} and query {Query}.", "HttpGet", nameof(GetPayload), header, query);
        var data = new SampleData { SomeInt = 42, SomeDate = DateTime.UtcNow };
        HttpContext.Response.Headers.Add("Method", nameof(GetPayload));
        return Task.FromResult<IActionResult>(Ok(new { header, query, data }));
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