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
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    // GET: api/policy
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<PolicyResponse>>> GetPolicies()
    {
        var policies = await _policyService.GetPoliciesAsync();
        return Ok(policies);
    }

    // GET: api/policy/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<PolicyResponse>> GetPolicy(Guid id)
    {
        var policy = await _policyService.GetPolicyAsync(id);
        return policy is null ? NotFound() : Ok(policy);
    }

    // POST: api/policy
    [HttpPost, EnableRateLimiting("writes")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PolicyResponse>> CreatePolicy(CreatePolicyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var policy = await _policyService.CreatePolicyAsync(request);
        return CreatedAtAction(nameof(GetPolicy), new { id = policy!.Id }, policy);
    }

    // PUT: api/policy/{id}
    [HttpPut("{id}"), EnableRateLimiting("writes")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PolicyResponse>> UpdatePolicy(Guid id, UpdatePolicyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var policy = await _policyService.UpdatePolicyAsync(id, request);
        return policy is null ? NotFound() : Ok(policy);
    }

    // PATCH: api/policy/{id}/toggle
    [HttpPatch("{id}/toggle"), EnableRateLimiting("writes")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        await _policyService.ToggleActiveAsync(id);
        return Ok();
    }

    // DELETE: api/policy/{id}
    [HttpDelete("{id}"), EnableRateLimiting("writes")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        await _policyService.DeletePolicyAsync(id);
        return NoContent();
    }
}
