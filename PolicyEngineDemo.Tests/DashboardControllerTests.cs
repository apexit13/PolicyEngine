using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PolicyEngine.Api.Controllers;
using PolicyEngineDemo.Shared.Interfaces;
using PolicyEngineDemo.Shared.Responses;
using System.Security.Claims;

namespace PolicyEngineDemo.Tests;

public class DashboardControllerTests
{
    private static DashboardController CreateController(IDashboardService dashboardService)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new("tenant_id", "tenant-1") // Consistent with your TenantId claim name
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new DashboardController(dashboardService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk_WithDashboardData()
    {
        // Arrange
        var expectedResponse = new DashboardResponse
        {
            TotalPolicies = 10,
            ActivePolicies = 8,
            RecentPolicies = new List<PolicyResponse> { new() { Title = "Latest" } }
        };

        var service = new Mock<IDashboardService>();
        service.Setup(s => s.GetDashboardAsync()).ReturnsAsync(expectedResponse);

        var controller = CreateController(service.Object);

        // Act
        var result = await controller.GetDashboard();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, ok.Value);
    }
}
