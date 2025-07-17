using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers;

[ApiController]
[Authorize]
[Route("Api/[controller]")]
public class ProtectedController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok("Some valid response.");
    }
}