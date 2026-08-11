using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = Policies.OnlyAdmin)]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly MetricsService _metricsService;

        public MetricsController(MetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        /// <summary>
        /// Aggregate figures for the admin dashboard: revenue, membership, class occupancy,
        /// attendance and per-trainer activity.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MetricsResponse>> Get()
        {
            return Ok(await _metricsService.GetMetricsAsync());
        }
    }
}
