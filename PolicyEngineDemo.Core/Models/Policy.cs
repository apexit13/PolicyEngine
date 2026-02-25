using PolicyEngineDemo.Core.Interfaces;

namespace PolicyEngineDemo.Core.Models;

public class Policy : IBaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Core Business Data
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Infrastructure Shadow Metadata
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
