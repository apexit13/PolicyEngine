using Microsoft.EntityFrameworkCore;
using PolicyEngine.Core.Data;
using PolicyEngine.Core.Models;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Core.Services;

/// <summary>
/// EF Core implementation of IDashboardService.
/// All queries are scoped to the authenticated tenant via AppDbContext's global query filter.
/// AuditLogs have no global filter — queried with explicit TenantId where clause.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        // Single round-trip for policy data (global query filter scopes to tenant)
        var policies = await _context.Policies
            .AsNoTracking()
            .ToListAsync();

        var total    = policies.Count;
        var active   = policies.Count(p => p.IsActive);
        var inactive = total - active;
        var rate     = total > 0 ? (int)Math.Round(active * 100.0 / total) : 0;

        var recent = policies
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(MapPolicyToResponse)
            .ToList();

        var byDate = policies
            .GroupBy(p => p.CreatedAt.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());

        // AuditLogs have no global filter — scope to tenant explicitly
        var tenantId = policies.FirstOrDefault()?.TenantId ?? "";
        var recentLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(5)
            .ToListAsync();

        return new DashboardResponse
        {
            TotalPolicies    = total,
            ActivePolicies   = active,
            InactivePolicies = inactive,
            ActiveRatePercent = rate,
            RecentPolicies   = recent,
            PoliciesByDate   = byDate,
            RecentAuditLogs  = recentLogs.Select(MapAuditToResponse).ToList()
        };
    }

    // ── Private mappings ─────────────────────────────────────────────────────

    private static PolicyResponse MapPolicyToResponse(Policy p) => new()
    {
        Id          = p.Id,
        Title       = p.Title,
        Description = p.Description,
        IsActive    = p.IsActive,
        TenantId    = p.TenantId,
        CreatedAt   = p.CreatedAt,
        CreatedBy   = p.CreatedBy
    };

    private static AuditLogResponse MapAuditToResponse(AuditLog a) => new()
    {
        TimestampUtc = a.TimestampUtc,
        Action       = DeriveAction(a.Method, a.Endpoint),
        Endpoint     = a.Endpoint,
        UserId       = a.UserId,
        StatusCode   = a.StatusCode,
        DurationMs   = a.DurationMs
    };

    /// <summary>
    /// Derives a human-readable action label from the HTTP method and endpoint.
    /// e.g. POST /api/policy       -> "Created"
    ///      PUT  /api/policy/{id}  -> "Updated"
    ///      PATCH /api/policy/{id}/toggle -> "Toggled"
    ///      DELETE /api/policy/{id} -> "Deleted"
    ///      GET  /api/policy       -> "Viewed"
    /// </summary>
    private static string DeriveAction(string method, string endpoint) =>
        method.ToUpperInvariant() switch
        {
            "POST"   => "Created",
            "PUT"    => "Updated",
            "PATCH"  => "Toggled",
            "DELETE" => "Deleted",
            "GET"    => "Viewed",
            _        => method
        };
}
