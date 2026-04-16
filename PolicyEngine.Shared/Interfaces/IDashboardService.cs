using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Shared.Interfaces;

/// <summary>
/// Dashboard aggregation contract.
/// API implementation (EF Core): PolicyEngine.Persistence.Services.DashboardService
/// Web implementation (HttpClient): PolicyEngine.Web.Services.DashboardService
/// </summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync();
}
