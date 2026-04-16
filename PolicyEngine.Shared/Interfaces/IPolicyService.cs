using PolicyEngine.Shared.Requests;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Shared.Interfaces;

/// <summary>
/// Shared policy service contract.
/// API implementation (EF Core): PolicyEngine.Persistence.Services.PolicyService
/// Web implementation (HttpClient): PolicyEngine.Web.Services.PolicyService
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
