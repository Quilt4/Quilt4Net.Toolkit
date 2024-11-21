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
        [Route("summary")]
        public async Task<IActionResult> GetSummary(string environment = default)
        {
            if (string.IsNullOrEmpty(environment)) environment = "Production";

            var result = await _applicationInsightsClient.GetSummaryAsync(environment).ToArrayAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("details")]
        public async Task<IActionResult> GetDetails(string environment = default, string appRoleName = default, string problemId = default)
        {
            if (string.IsNullOrEmpty(environment)) environment = "Production";
            if (string.IsNullOrEmpty(appRoleName)) appRoleName = "app-eplicta-fido-prod";
            if (string.IsNullOrEmpty(problemId)) problemId = "System.InvalidCastException at Eplicta.Fido.Features.GetPublication.PublicationAccessNotifyAggregatorBehavior.UpdateCache";

            var result = await _applicationInsightsClient.GetDetails(environment, appRoleName, problemId);
            return Ok(result);
        }
    }
}