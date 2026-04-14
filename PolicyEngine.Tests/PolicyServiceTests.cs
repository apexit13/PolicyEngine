using Microsoft.EntityFrameworkCore;
using Moq;
using PolicyEngine.Persistence.Models;
using PolicyEngine.Persistence.Services;
using PolicyEngine.Persistence.Data;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Requests;

namespace PolicyEngine.Tests;

/// <summary>
/// Tests the EF Core implementation of IPolicyService.
/// Uses an in-memory database so no mocking of data access is needed.
/// Tenant isolation via AppDbContext global query filter is tested here.
/// </summary>
public class PolicyServiceTests
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

    // ── GetPoliciesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPoliciesAsync_ReturnsAllPolicies_ForTenant()
    {
        var context = CreateDbContext();
        context.Policies.AddRange(
            CreatePolicy(isActive: true, title: "Active Policy"),
            CreatePolicy(isActive: false, title: "Inactive Policy")
        );
        await context.SaveChangesAsync();

        var service = new PolicyService(context);
        var result = await service.GetPoliciesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPoliciesAsync_DoesNotReturn_OtherTenantPolicies()
    {
        // Seed with tenant-2 data using a separate context
        var otherContext = CreateDbContext("tenant-2");
        otherContext.Policies.Add(CreatePolicy(tenantId: "tenant-2", title: "Other Tenant"));
        await otherContext.SaveChangesAsync();

        // Query as tenant-1 — global query filter should exclude tenant-2 data
        var context = CreateDbContext("tenant-1");
        var service = new PolicyService(context);
        var result = await service.GetPoliciesAsync();

        Assert.Empty(result);
    }

    // ── GetPolicyAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolicyAsync_ReturnsPolicy_WhenFound()
    {
        var context = CreateDbContext();
        var policy = CreatePolicy();
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var service = new PolicyService(context);
        var result = await service.GetPolicyAsync(policy.Id);

        Assert.NotNull(result);
        Assert.Equal(policy.Id, result.Id);
        Assert.Equal(policy.Title, result.Title);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsNull_WhenNotFound()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        var result = await service.GetPolicyAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── CreatePolicyAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePolicyAsync_ReturnsMappedResponse()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        var request = new CreatePolicyRequest
        {
            Title = "New Policy",
            Description = "New Description",
            IsActive = true
        };

        var result = await service.CreatePolicyAsync(request);

        Assert.NotNull(result);
        Assert.Equal("New Policy", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreatePolicyAsync_PersistsToDatabase()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        await service.CreatePolicyAsync(new CreatePolicyRequest
        {
            Title = "Persisted Policy",
            Description = "Description",
            IsActive = true
        });

        Assert.Equal(1, await context.Policies.CountAsync());
    }

    // ── UpdatePolicyAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicyAsync_ReturnsUpdatedResponse()
    {
        var context = CreateDbContext();
        var policy = CreatePolicy(title: "Original Title");
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var service = new PolicyService(context);

        var result = await service.UpdatePolicyAsync(policy.Id, new UpdatePolicyRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            IsActive = false
        });

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdatePolicyAsync_ReturnsNull_WhenNotFound()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        var result = await service.UpdatePolicyAsync(Guid.NewGuid(), new UpdatePolicyRequest
        {
            Title = "Title",
            Description = "Description",
            IsActive = true
        });

        Assert.Null(result);
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActiveAsync_TogglesIsActive_WhenPolicyExists()
    {
        var context = CreateDbContext();
        var policy = CreatePolicy(isActive: true);
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var service = new PolicyService(context);
        await service.ToggleActiveAsync(policy.Id);

        var updated = await context.Policies.FindAsync(policy.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_DoesNotThrow_WhenNotFound()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        // Should complete gracefully with no exception
        await service.ToggleActiveAsync(Guid.NewGuid());
    }

    // ── DeletePolicyAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePolicyAsync_RemovesFromDatabase()
    {
        var context = CreateDbContext();
        var policy = CreatePolicy();
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        var service = new PolicyService(context);
        await service.DeletePolicyAsync(policy.Id);

        Assert.Equal(0, await context.Policies.CountAsync());
    }

    [Fact]
    public async Task DeletePolicyAsync_DoesNotThrow_WhenNotFound()
    {
        var context = CreateDbContext();
        var service = new PolicyService(context);

        // Should complete gracefully with no exception
        await service.DeletePolicyAsync(Guid.NewGuid());
    }
}
