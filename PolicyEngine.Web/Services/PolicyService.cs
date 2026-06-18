using System.Net.Http.Json;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Requests;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Web.Services;

/// <summary>
/// Wraps all API calls to PolicyEngine.Api.
/// The HttpClient is pre-configured in Program.cs to attach the Auth0
/// bearer token on every request automatically.
/// </summary>
public class PolicyService : IPolicyService
{
    private readonly HttpClient _http;

    public PolicyService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<PolicyResponse>> GetPoliciesAsync()
    {
        return await _http.GetFromJsonAsync<List<PolicyResponse>>("api/policies")
               ?? [];
    }

    public async Task<PolicyResponse?> GetPolicyAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<PolicyResponse>($"api/policies/{id}");
    }

    public async Task<PolicyResponse?> CreatePolicyAsync(CreatePolicyRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/policies", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolicyResponse>();
    }

    public async Task<PolicyResponse?> UpdatePolicyAsync(Guid id, UpdatePolicyRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/policies/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolicyResponse>();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var response = await _http.PatchAsync($"api/policies/{id}/toggle", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeletePolicyAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/policies/{id}");
        response.EnsureSuccessStatusCode();
    }
}
