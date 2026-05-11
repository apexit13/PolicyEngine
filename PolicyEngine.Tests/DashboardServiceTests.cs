using Microsoft.EntityFrameworkCore;
using Moq;
using PolicyEngine.Persistence.Models;
using PolicyEngine.Persistence.Services;
using PolicyEngine.Persistence.Data;
using PolicyEngine.Shared.Interfaces;

namespace PolicyEngine.Tests;

public class DashboardServiceTests
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

    private static Policy CreatePolicy(string tenantId, bool isActive, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Policy",
        TenantId = tenantId,
        IsActive = isActive,
        CreatedAt = createdAt
    };

    private static AuditLog CreateAuditLog(string tenantId, string method, long id = 0) => new()
    {
        // If Id is 0, EF Core will treat it as a new entity and auto-generate the long
        Id = id,
        TenantId = tenantId,
        Method = method,
        Endpoint = "/api/test",
        TimestampUtc = DateTime.UtcNow,
        UserId = "user-1"
    };

    [Fact]
    public async Task GetDashboardAsync_CalculatesStatistics_Correctly()
    {
        // Arrange
        var context = CreateDbContext("tenant-1");
        context.Policies.AddRange(
            CreatePolicy("tenant-1", true, DateTime.UtcNow),
            CreatePolicy("tenant-1", true, DateTime.UtcNow.AddDays(-1)),
            CreatePolicy("tenant-1", false, DateTime.UtcNow.AddDays(-2))
        );
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        Assert.Equal(3, result.TotalPolicies);
        Assert.Equal(2, result.ActivePolicies);
        Assert.Equal(1, result.InactivePolicies);
        Assert.Equal(67, result.ActiveRatePercent); // (2/3) * 100 rounded
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersAuditLogs_ByTenant()
    {
        // Arrange
        var context = CreateDbContext("tenant-1");

        // Add policy to establish the tenant context
        context.Policies.Add(CreatePolicy("tenant-1", true, DateTime.UtcNow));

        // Use distinct long IDs or let them auto-generate
        context.AuditLogs.AddRange(
            CreateAuditLog("tenant-1", "GET", 101),
            CreateAuditLog("tenant-2", "POST", 102)
        );
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        Assert.Single(result.RecentAuditLogs);
        Assert.Equal("Viewed", result.RecentAuditLogs.First().Action);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsRecentItems_LimitedToFive()
    {
        // Arrange
        var context = CreateDbContext("tenant-1");
        for (int i = 0; i < 10; i++)
        {
            context.Policies.Add(CreatePolicy("tenant-1", true, DateTime.UtcNow.AddHours(i)));
        }
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        Assert.Equal(5, result.RecentPolicies.Count);
        // Verify ordering (most recent first)
        Assert.True(result.RecentPolicies[0].CreatedAt > result.RecentPolicies[1].CreatedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsEmpty_WhenNoDataExists()
    {
        // Arrange
        var context = CreateDbContext("tenant-1");
        var service = new DashboardService(context);

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        Assert.Equal(0, result.TotalPolicies);
        Assert.Empty(result.RecentPolicies);
        Assert.Empty(result.RecentAuditLogs);
    }
}