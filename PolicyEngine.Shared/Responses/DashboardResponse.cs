namespace PolicyEngine.Shared.Responses;

/// <summary>
/// Returned by GET /api/dashboard.
/// Contains aggregated stats for the Dashboard page.
/// </summary>
public class DashboardResponse
{
    public int TotalPolicies     { get; set; }
    public int ActivePolicies    { get; set; }
    public int InactivePolicies  { get; set; }

    /// <summary>Percentage of policies that are active, 0-100.</summary>
    public int ActiveRatePercent { get; set; }

    /// <summary>The 5 most recently created policies, newest first.</summary>
    public List<PolicyResponse> RecentPolicies { get; set; } = [];

    /// <summary>
    /// Policies grouped by calendar date for the bar chart.
    /// Key = "yyyy-MM-dd", Value = count of policies created on that date.
    /// </summary>
    public Dictionary<string, int> PoliciesByDate { get; set; } = [];

    /// <summary>The 5 most recent audit log entries, newest first.</summary>
    public List<AuditLogResponse> RecentAuditLogs { get; set; } = [];
}
