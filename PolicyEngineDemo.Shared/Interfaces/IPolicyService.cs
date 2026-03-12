using PolicyEngineDemo.Shared.Requests;
using PolicyEngineDemo.Shared.Responses;

namespace PolicyEngineDemo.Shared.Interfaces;

/// <summary>
/// Shared policy service contract.
/// API implementation (EF Core): PolicyEngineDemo.Core.Services.PolicyService
/// Web implementation (HttpClient): PolicyEngineDemo.Web.Services.PolicyService
/// </summary>
public interface IPolicyService
{
    Task<List<PolicyResponse>> GetPoliciesAsync();
    Task<PolicyResponse?> GetPolicyAsync(Guid id);
    Task<PolicyResponse?> CreatePolicyAsync(CreatePolicyRequest request);
    Task<PolicyResponse?> UpdatePolicyAsync(Guid id, UpdatePolicyRequest request);
    Task ToggleActiveAsync(Guid id);
    Task DeletePolicyAsync(Guid id);
}
