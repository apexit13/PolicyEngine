using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyEngineDemo.Shared.Interfaces;
using PolicyEngineDemo.Shared.Responses;

namespace PolicyEngine.Api.Controllers;

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

    // GET: api/dashboard
    [HttpGet]
    [Authorize(Policy = "Policy.Viewer")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard()
    {
        var dashboard = await _dashboardService.GetDashboardAsync();
        return Ok(dashboard);
    }
}
