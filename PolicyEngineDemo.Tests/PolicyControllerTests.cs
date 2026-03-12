using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PolicyEngine.Api.Controllers;
using PolicyEngineDemo.Contracts.Constants;
using PolicyEngineDemo.Contracts.Data;
using PolicyEngineDemo.Contracts.Interfaces;
using PolicyEngineDemo.Contracts.Models;
using PolicyEngineDemo.Contracts.Requests;
using PolicyEngineDemo.Contracts.Responses;
using System.Security.Claims;

namespace PolicyEngineDemo.Tests;

public class PolicyControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateDbContext(string tenantId = "tenant-1")
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.TenantId()).Returns(tenantId);
        tenantProvider.Setup(t => t.UserId()).Returns("user-1");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, tenantProvider.Object);
    }

    private static PolicyController CreateController(
        AppDbContext context,
        bool isAdmin = false,
        string tenantId = "tenant-1")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimNames.TenantId, tenantId)
        };

        if (isAdmin)
            claims.Add(new Claim(ClaimNames.Roles, "Policy.Admin"));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new PolicyController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        return controller;
    }

    private static Policy CreatePolicy(
        string tenantId = "tenant-1",
        bool isActive = true,
        string title = "Test Policy") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "Test description",
        IsActive = isActive,
        TenantId = tenantId,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "user-1"
    };

    // ── GetPolicies ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicies_AsViewer_ReturnsOnlyActivePolicies()
    {
        // Arrange
        var context = CreateDbContext();
        context.Policies.AddRange(
            CreatePolicy(isActive: true, title: "Active Policy"),
            CreatePolicy(isActive: false, title: "Inactive Policy")
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: false);

        // Act
        var result = await controller.GetPolicies();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var policies = Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value);
        Assert.Single(policies);
        Assert.Equal("Active Policy", policies.First().Title);
    }

    [Fact]
    public async Task GetPolicies_AsAdmin_ReturnsAllPolicies()
    {
        // Arrange
        var context = CreateDbContext();
        context.Policies.AddRange(
            CreatePolicy(isActive: true, title: "Active Policy"),
            CreatePolicy(isActive: false, title: "Inactive Policy")
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.GetPolicies();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var policies = Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value);
        Assert.Equal(2, policies.Count());
    }

    [Fact]
    public async Task GetPolicies_ReturnsEmpty_WhenNoPoliciesExist()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.GetPolicies();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var policies = Assert.IsAssignableFrom<IEnumerable<PolicyResponse>>(ok.Value);
        Assert.Empty(policies);
    }

    // ── GetPolicy ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicy_ReturnsPolicy_WhenFound()
    {
        // Arrange
        var context = CreateDbContext();
        var policy = CreatePolicy();
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        // Act
        var result = await controller.GetPolicy(policy.Id);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PolicyResponse>(ok.Value);
        Assert.Equal(policy.Id, response.Id);
        Assert.Equal(policy.Title, response.Title);
    }

    [Fact]
    public async Task GetPolicy_ReturnsNotFound_WhenPolicyDoesNotExist()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.GetPolicy(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── CreatePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePolicy_ReturnsCreated_WithNewPolicy()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        var request = new CreatePolicyRequest
        {
            Title = "New Policy",
            Description = "New Description",
            IsActive = true
        };

        // Act
        var result = await controller.CreatePolicy(request);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<PolicyResponse>(created.Value);
        Assert.Equal("New Policy", response.Title);
        Assert.Equal("New Description", response.Description);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task CreatePolicy_PersistsToDatabase()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        var request = new CreatePolicyRequest
        {
            Title = "Persisted Policy",
            Description = "Description",
            IsActive = true
        };

        // Act
        await controller.CreatePolicy(request);

        // Assert
        Assert.Equal(1, await context.Policies.CountAsync());
    }

    // ── UpdatePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicy_ReturnsOk_WithUpdatedPolicy()
    {
        // Arrange
        var context = CreateDbContext();
        var policy = CreatePolicy(title: "Original Title");
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: true);

        var request = new UpdatePolicyRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            IsActive = false
        };

        // Act
        var result = await controller.UpdatePolicy(policy.Id, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PolicyResponse>(ok.Value);
        Assert.Equal("Updated Title", response.Title);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task UpdatePolicy_ReturnsNotFound_WhenPolicyDoesNotExist()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        var request = new UpdatePolicyRequest
        {
            Title = "Title",
            Description = "Description",
            IsActive = true
        };

        // Act
        var result = await controller.UpdatePolicy(Guid.NewGuid(), request);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── ToggleActive ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_TogglesIsActive_WhenPolicyExists()
    {
        // Arrange
        var context = CreateDbContext();
        var policy = CreatePolicy(isActive: true);
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.ToggleActive(policy.Id);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var updated = await context.Policies.FindAsync(policy.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task ToggleActive_ReturnsNotFound_WhenPolicyDoesNotExist()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.ToggleActive(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ── DeletePolicy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePolicy_ReturnsNoContent_WhenPolicyExists()
    {
        // Arrange
        var context = CreateDbContext();
        var policy = CreatePolicy();
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.DeletePolicy(policy.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePolicy_RemovesFromDatabase()
    {
        // Arrange
        var context = CreateDbContext();
        var policy = CreatePolicy();
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var controller = CreateController(context, isAdmin: true);

        // Act
        await controller.DeletePolicy(policy.Id);

        // Assert
        Assert.Equal(0, await context.Policies.CountAsync());
    }

    [Fact]
    public async Task DeletePolicy_ReturnsNotFound_WhenPolicyDoesNotExist()
    {
        // Arrange
        var context = CreateDbContext();
        var controller = CreateController(context, isAdmin: true);

        // Act
        var result = await controller.DeletePolicy(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
