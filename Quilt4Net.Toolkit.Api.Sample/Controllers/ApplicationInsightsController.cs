using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

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
        [Route("summary")]
        public async Task<IActionResult> GetSummary(string environment)
        {
            if (string.IsNullOrEmpty(environment)) environment = "Production";

            var result = await _applicationInsightsClient.GetSummaryAsync(environment).ToArrayAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("details")]
        public async Task<IActionResult> GetDetails(string environment, string summaryIdentifier)
        {
            if (string.IsNullOrEmpty(environment)) environment = "Production";

            var result = await _applicationInsightsClient.GetDetails(environment, summaryIdentifier);
            return Ok(result);
        }
    }
}