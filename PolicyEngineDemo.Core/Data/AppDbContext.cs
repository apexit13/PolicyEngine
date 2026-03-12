using Microsoft.EntityFrameworkCore;
using PolicyEngineDemo.Core.Models;
using PolicyEngineDemo.Shared.Interfaces;

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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filter — every Policies query automatically scopes to
        // the current tenant. Users physically cannot see other tenants' data.
        modelBuilder.Entity<Policy>()
            .HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId());

        // AuditLogs: no global filter — admins querying audit logs can see all
        // entries for their tenant via explicit Where() in the controller.
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.ToTable("AuditLogs");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-stamp TenantId, CreatedAt, CreatedBy on new Policy entities
        foreach (var entry in ChangeTracker.Entries<Policy>()
            .Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = _tenantProvider.TenantId() ?? "";
            entry.Entity.CreatedAt = DateTime.UtcNow;
            entry.Entity.CreatedBy = _tenantProvider.UserId() ?? "";
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
