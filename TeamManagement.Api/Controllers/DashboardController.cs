using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManagement.Api.Extensions;
using TeamManagement.Application.Interfaces;

namespace TeamManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("ekip/{ekipId}")]
        public async Task<IActionResult> GetDashboardByEkipId(Guid ekipId)
        {
            var callerEkipId = User.GetEkipId();
            if (callerEkipId == null || callerEkipId != ekipId)
                return Forbid();

            var result = await _dashboardService.GetDashboardByEkipIdAsync(ekipId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
