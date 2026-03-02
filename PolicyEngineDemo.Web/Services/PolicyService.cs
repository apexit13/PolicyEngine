using System.Net.Http.Json;
using PolicyEngineDemo.Web.Models;

namespace PolicyEngineDemo.Web.Services;

/// <summary>
/// Wraps all API calls to PolicyEngineDemo.Api.
/// The HttpClient is pre-configured in Program.cs to attach the Auth0
/// bearer token on every request automatically.
/// </summary>
public class PolicyService
{
    private readonly HttpClient _http;

    public PolicyService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Policy>> GetPoliciesAsync()
    {
        return await _http.GetFromJsonAsync<List<Policy>>("api/policy")
               ?? [];
    }

    public async Task<Policy?> GetPolicyAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<Policy>($"api/policy/{id}");
    }

    public async Task<Policy?> CreatePolicyAsync(PolicyDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/policy", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Policy>();
    }

    public async Task<Policy?> UpdatePolicyAsync(Guid id, PolicyDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/policy/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Policy>();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var response = await _http.PatchAsync($"api/policy/{id}/toggle", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeletePolicyAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/policy/{id}");
        response.EnsureSuccessStatusCode();
    }
}
