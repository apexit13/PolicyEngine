using Microsoft.EntityFrameworkCore;
using PolicyEngineDemo.Core.Interfaces;
using PolicyEngineDemo.Core.Models;

namespace PolicyEngineDemo.Core.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- GLOBAL QUERY FILTER ---
        // Every 'Select' query automatically adds: WHERE TenantId = 'CurrentTenantId'
        modelBuilder.Entity<Policy>().HasQueryFilter(p => p.TenantId == _tenantProvider.GetTenantId());

        // TenantId is indexed for high-speed lookups in Azure SQL
        modelBuilder.Entity<Policy>().HasIndex(p => p.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // --- AUTOMATIC INJECTION ---
        foreach (var entry in ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantProvider.GetTenantId() ?? "Unknown";
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _tenantProvider.GetUserId() ?? "System";
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
