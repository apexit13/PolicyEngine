using PolicyEngine.Shared.Interfaces;

namespace PolicyEngine.Persistence.Models;

public class Policy : IBaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Core Business Data
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Handled as Shadow Properties in EF Core
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
