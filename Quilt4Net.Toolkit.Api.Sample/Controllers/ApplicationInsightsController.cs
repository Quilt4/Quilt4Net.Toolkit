using Microsoft.AspNetCore.Mvc;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApplicationInsightsController : ControllerBase
    {
        private readonly IApplicationInsightsClient _applicationInsightsClient;

        public ApplicationInsightsController(IApplicationInsightsClient applicationInsightsClient)
        {
            _applicationInsightsClient = applicationInsightsClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicationInsightsSummary()
        {
            var result = await _applicationInsightsClient.GetSummaryAsync("Production").ToArrayAsync();
            return Ok(result);
        }
    }
}