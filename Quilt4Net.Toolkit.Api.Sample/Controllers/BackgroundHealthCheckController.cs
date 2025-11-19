using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Sample;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class BackgroundHealthCheckController : ControllerBase
{
    private readonly BackgroundHealthCheckService _class1;

    public BackgroundHealthCheckController(BackgroundHealthCheckService class1)
    {
        _class1 = class1;
    }
    [HttpGet]
    public async Task<IActionResult> Check()
    {
        await _class1.Heartbeat();
        return Ok();
    }
}