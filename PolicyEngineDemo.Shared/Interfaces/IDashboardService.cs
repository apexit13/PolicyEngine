using PolicyEngineDemo.Shared.Responses;

namespace PolicyEngineDemo.Shared.Interfaces;

/// <summary>
/// Dashboard aggregation contract.
/// API implementation (EF Core): PolicyEngineDemo.Core.Services.DashboardService
/// Web implementation (HttpClient): PolicyEngineDemo.Web.Services.DashboardService
/// </summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync();
}
