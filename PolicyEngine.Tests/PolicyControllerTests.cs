using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PolicyEngine.Api.Controllers;
using PolicyEngine.Authorization.Constants;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Requests;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Tests;

/// <summary>
/// Tests PolicyController HTTP response mapping.
/// IPolicyService is mocked — business logic is tested in PolicyServiceTests.
/// </summary>
public class PolicyControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PolicyController CreateController(
        IPolicyService policyService,
        bool isAdmin = false,
        string tenantId = "tenant-1")
    {
        var claims = new List<Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "user-1"),
            new(AuthClaimTypes.TenantId, tenantId)
        };

        //if (isAdmin)
        //    claims.Add(new Claim(ClaimTypes.Roles, "policy.admin"));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new PolicyController(policyService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static PolicyResponse MakePolicyResponse(
        string title = "Test Policy",
        bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            IsActive = isActive,
            TenantId = "tenant-1",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user-1"
        };

    // ── GetPolicies ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicies_ReturnsOk_WithPolicies()
    {
        var policies = new List<PolicyResponse>
        {
            MakePolicyResponse("Policy A"),
            MakePolicyResponse("Policy B")
        };

        var service = new Mock<IPolicyService>();
        service.Setup(s => s.GetPoliciesAsync()).ReturnsAsync(policies);

        var controller = CreateController(service.Object);
        var result = await controller.GetPolicies();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetPolicies_ReturnsOk_WithEmptyList()
    {
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.GetPoliciesAsync()).ReturnsAsync([]);

        var controller = CreateController(service.Object);
        var result = await controller.GetPolicies();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value);
        Assert.Empty(returned);
    }

    // ── GetPolicy ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicy_ReturnsOk_WhenFound()
    {
        var policy = MakePolicyResponse();
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.GetPolicyAsync(policy.Id)).ReturnsAsync(policy);

        var controller = CreateController(service.Object);
        var result = await controller.GetPolicy(policy.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(policy, ok.Value);
    }

    [Fact]
    public async Task GetPolicy_ReturnsNotFound_WhenNull()
    {
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.GetPolicyAsync(It.IsAny<Guid>())).ReturnsAsync((PolicyResponse?)null);

        var controller = CreateController(service.Object);
        var result = await controller.GetPolicy(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── CreatePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePolicy_ReturnsCreated_WithPolicy()
    {
        var policy = MakePolicyResponse("New Policy");
        var request = new CreatePolicyRequest
        {
            Title = "New Policy",
            Description = "Description",
            IsActive = true
        };

        var service = new Mock<IPolicyService>();
        service.Setup(s => s.CreatePolicyAsync(request)).ReturnsAsync(policy);

        var controller = CreateController(service.Object, isAdmin: true);
        var result = await controller.CreatePolicy(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(policy, created.Value);
    }

    // ── UpdatePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicy_ReturnsOk_WhenFound()
    {
        var policy = MakePolicyResponse("Updated");
        var request = new UpdatePolicyRequest
        {
            Title = "Updated",
            Description = "Description",
            IsActive = true
        };

        var service = new Mock<IPolicyService>();
        service.Setup(s => s.UpdatePolicyAsync(policy.Id, request)).ReturnsAsync(policy);

        var controller = CreateController(service.Object, isAdmin: true);
        var result = await controller.UpdatePolicy(policy.Id, request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(policy, ok.Value);
    }

    [Fact]
    public async Task UpdatePolicy_ReturnsNotFound_WhenNull()
    {
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.UpdatePolicyAsync(It.IsAny<Guid>(), It.IsAny<UpdatePolicyRequest>()))
               .ReturnsAsync((PolicyResponse?)null);

        var controller = CreateController(service.Object, isAdmin: true);
        var result = await controller.UpdatePolicy(Guid.NewGuid(), new UpdatePolicyRequest
        {
            Title = "Title",
            Description = "Description",
            IsActive = true
        });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── ToggleActive ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_ReturnsOk()
    {
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.ToggleActiveAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        var controller = CreateController(service.Object, isAdmin: true);
        var result = await controller.ToggleActive(Guid.NewGuid());

        Assert.IsType<OkResult>(result);
    }

    // ── DeletePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePolicy_ReturnsNoContent()
    {
        var service = new Mock<IPolicyService>();
        service.Setup(s => s.DeletePolicyAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        var controller = CreateController(service.Object, isAdmin: true);
        var result = await controller.DeletePolicy(Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }
}
