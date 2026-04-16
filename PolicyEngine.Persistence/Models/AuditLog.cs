namespace PolicyEngine.Persistence.Models;

public class AuditLog
{
    public long Id { get; set; }                  // bigint identity — high volume
    public DateTime TimestampUtc { get; set; }
    public string Method { get; set; } = "";      // GET, POST, PUT, PATCH, DELETE
    public string Endpoint { get; set; } = "";    // /api/policy, /api/policy/{id}, etc.
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string? RequestBody { get; set; }      // null for GET/DELETE
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
}
