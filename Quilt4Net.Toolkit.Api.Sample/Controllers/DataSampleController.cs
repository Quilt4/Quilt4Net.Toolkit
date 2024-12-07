using Microsoft.AspNetCore.Mvc;

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
        //_logger.LogInformation("This is an informational log {header}.", header);
        //_logger.LogWarning("This is a warning log {header}.", header);
        //_logger.LogError("This is an error log {header}.", header);

        HttpContext.Response.Headers.Add("Method", nameof(PostPayload));
        return Ok(new { header, query, data });
    }

    [HttpGet]
    public async Task<IActionResult> GetPayload([FromHeader] string header, [FromQuery] string query)
    {
        var data = new SampleData { SomeInt = 42, SomeDate = DateTime.UtcNow };
        HttpContext.Response.Headers.Add("Method", nameof(GetPayload));
        return Ok(new { header, query, data });
    }
}