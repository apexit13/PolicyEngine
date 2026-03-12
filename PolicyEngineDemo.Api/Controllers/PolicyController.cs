using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicyEngineDemo.Shared.Constants;
using PolicyEngineDemo.Shared.Data;
using PolicyEngineDemo.Shared.Models;
using PolicyEngineDemo.Shared.Requests;
using PolicyEngineDemo.Shared.Responses;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PolicyController : ControllerBase
{
    private readonly AppDbContext _context;

    public PolicyController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/policy
    [HttpGet]
    [Authorize(Policy = "Policy.Viewer")]
    public async Task<ActionResult<IEnumerable<PolicyResponse>>> GetPolicies()
    {
        var isAdmin = User.HasClaim(ClaimNames.Roles, "Policy.Admin");

        var query = _context.Policies.AsQueryable();

        if (!isAdmin)
            query = query.Where(p => p.IsActive);

        var policies = await query.ToListAsync();

        return Ok(policies.Select(MapToResponse));
    }

    // GET: api/policy/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "Policy.Viewer")]
    public async Task<ActionResult<PolicyResponse>> GetPolicy(Guid id)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return NotFound();

        return Ok(MapToResponse(policy));
    }

    // POST: api/policy
    [HttpPost]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<ActionResult<PolicyResponse>> CreatePolicy(
        CreatePolicyRequest request)
    {
        var policy = new Policy
        {
            Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPolicy),
            new { id = policy.Id },
            MapToResponse(policy));
    }

    // PUT: api/policy/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<ActionResult<PolicyResponse>> UpdatePolicy(
        Guid id, UpdatePolicyRequest request)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return NotFound();

        policy.Title = request.Title;
        policy.Description = request.Description;
        policy.IsActive = request.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Policies.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }

        return Ok(MapToResponse(policy));
    }

    // PATCH: api/policy/{id}/toggle
    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return NotFound();

        policy.IsActive = !policy.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { policy.Id, policy.IsActive });
    }

    // DELETE: api/policy/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return NotFound();

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ── Private mapping ──────────────────────────────────────────────────────
    private static PolicyResponse MapToResponse(Policy p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        IsActive = p.IsActive,
        TenantId = p.TenantId,
        CreatedAt = p.CreatedAt,
        CreatedBy = p.CreatedBy
    };
}