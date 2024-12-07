using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.Measure;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("[controller]")]
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
        var data = new SampleData { SomeInt = 42, SomeDate = DateTime.UtcNow };
        HttpContext.Response.Headers.Add("Method", nameof(GetPayload));
        return Task.FromResult<IActionResult>(Ok(new { header, query, data }));
    }
}