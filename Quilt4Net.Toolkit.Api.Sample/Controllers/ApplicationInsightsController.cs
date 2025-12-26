using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.ApplicationInsights;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApplicationInsightsController : ControllerBase
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IApplicationInsightsService _applicationInsightsClient;

        public ApplicationInsightsController(IHostEnvironment hostEnvironment, IApplicationInsightsService applicationInsightsClient)
        {
            _hostEnvironment = hostEnvironment;
            _applicationInsightsClient = applicationInsightsClient;
        }

        [HttpGet]
        [Route("summary")]
        public async Task<IActionResult> GetSummary(string environment)
        {
            if (string.IsNullOrEmpty(environment)) environment = _hostEnvironment.EnvironmentName;

            var result = await _applicationInsightsClient.GetSummaryAsync(null, environment, TimeSpan.FromMinutes(15)).ToArrayAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("details")]
        public async Task<IActionResult> GetDetails(string environment, string summaryIdentifier)
        {
            if (string.IsNullOrEmpty(environment)) environment = _hostEnvironment.EnvironmentName;

            var result = await _applicationInsightsClient.GetDetails(environment, summaryIdentifier, TimeSpan.FromDays(7)).ToArrayAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("measurements")]
        public async Task<IActionResult> GetMeasurements(string environment)
        {
            if (string.IsNullOrEmpty(environment)) environment = _hostEnvironment.EnvironmentName;

            var result = await _applicationInsightsClient.GetMeasurements(environment, TimeSpan.FromMinutes(15)).ToArrayAsync();
            return Ok(result);
        }
    }
}