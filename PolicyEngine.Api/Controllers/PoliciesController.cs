using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Requests;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    // GET: api/policies
    [HttpGet]
    [Authorize(Policy = "ReadPolicies")]
    public async Task<ActionResult<IEnumerable<PolicyResponse>>> GetPolicies()
    {
        var policies = await _policyService.GetPoliciesAsync();
        return Ok(policies);
    }

    // GET: api/policies/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "ReadPolicies")]
    public async Task<ActionResult<PolicyResponse>> GetPolicy(Guid id)
    {
        var policy = await _policyService.GetPolicyAsync(id);
        return policy is null ? NotFound() : Ok(policy);
    }

    // POST: api/policies
    [HttpPost, EnableRateLimiting("writes")]
    [Authorize(Policy = "ManagePolicies")]
    public async Task<ActionResult<PolicyResponse>> CreatePolicy(CreatePolicyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var policy = await _policyService.CreatePolicyAsync(request);
        return CreatedAtAction(nameof(GetPolicy), new { id = policy!.Id }, policy);
    }

    // PUT: api/policies/{id}
    [HttpPut("{id}"), EnableRateLimiting("writes")]
    [Authorize(Policy = "ManagePolicies")]
    public async Task<ActionResult<PolicyResponse>> UpdatePolicy(Guid id, UpdatePolicyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var policy = await _policyService.UpdatePolicyAsync(id, request);
        return policy is null ? NotFound() : Ok(policy);
    }

    // PATCH: api/policies/{id}/toggle
    [HttpPatch("{id}/toggle"), EnableRateLimiting("writes")]
    [Authorize(Policy = "ManagePolicies")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        await _policyService.ToggleActiveAsync(id);
        return Ok();
    }

    // DELETE: api/policies/{id}
    [HttpDelete("{id}"), EnableRateLimiting("writes")]
    [Authorize(Policy = "ManagePolicies")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        await _policyService.DeletePolicyAsync(id);
        return NoContent();
    }
}
