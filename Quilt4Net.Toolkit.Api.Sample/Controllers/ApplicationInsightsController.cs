using Microsoft.AspNetCore.Mvc;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApplicationInsightsController : ControllerBase
    {
        private readonly IApplicationInsightsClient _applicationInsightsClient;
        private readonly IHealthClieht _healthClieht;

        public ApplicationInsightsController(IApplicationInsightsClient applicationInsightsClient, IHealthClieht healthClieht)
        {
            _applicationInsightsClient = applicationInsightsClient;
            _healthClieht = healthClieht;
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

        [HttpGet]
        [Route("live-proxy")]
        public async Task<IActionResult> GetLive(CancellationToken cancellationToken)
        {
            var result = await _healthClieht.GetLiveAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("ready-proxy")]
        public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
        {
            var result = await _healthClieht.GetReadyAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("health-proxy")]
        public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
        {
            var result = await _healthClieht.GetHealthAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("metrics-proxy")]
        public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
        {
            var result = await _healthClieht.GetMetricsAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("version-proxy")]
        public async Task<IActionResult> GetVersion(CancellationToken cancellationToken)
        {
            var result = await _healthClieht.GetVersionAsync(cancellationToken);
            return Ok(result);
        }

    }
}