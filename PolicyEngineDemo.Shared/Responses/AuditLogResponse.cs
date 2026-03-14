namespace PolicyEngineDemo.Shared.Responses;

/// <summary>
/// A single audit log entry surfaced on the Dashboard.
/// Action label is derived server-side from Method + Endpoint.
/// </summary>
public class AuditLogResponse
{
    public DateTime TimestampUtc { get; set; }
    public string   Action       { get; set; } = ""; // e.g. "Created", "Updated", "Deleted", "Toggled"
    public string   Endpoint     { get; set; } = ""; // e.g. "/api/policy"
    public string?  UserId       { get; set; }
    public int      StatusCode   { get; set; }
    public long     DurationMs   { get; set; }
}
