using System.Net.Http.Json;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Web.Services;

/// <summary>
/// HttpClient implementation of IDashboardService.
/// isAdmin is resolved server-side from the JWT — the API returns the
/// appropriate data based on the authenticated user's role.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly HttpClient _http;

    public DashboardService(HttpClient http)
    {
        _http = http;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        return await _http.GetFromJsonAsync<DashboardResponse>("api/dashboard")
               ?? new DashboardResponse();
    }
}
