using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicyEngineDemo.Api.DTOs;
using PolicyEngineDemo.Core.Data;
using PolicyEngineDemo.Core.Interfaces;
using PolicyEngineDemo.Core.Models;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require a valid Entra ID token minimum
public class PolicyController : ControllerBase
{
    private readonly AppDbContext _context;

    public PolicyController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/policy/debug
    [HttpGet("debug")]
    [AllowAnonymous]
    public IActionResult Debug()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            tenantFromProvider = HttpContext.RequestServices
                .GetRequiredService<ITenantProvider>().TenantId(),
            userIdFromProvider = HttpContext.RequestServices
                .GetRequiredService<ITenantProvider>().UserId(),
            claims
        });
    }

    // GET: api/policy
    // Both Admins and Viewers can read policies for their tenant.
    // The global query filter in AppDbContext ensures only the caller's
    // tenant data is returned. Admins see all their policies, but Viewers only see active ones.
    [HttpGet]
    [Authorize(Policy = "Policy.Viewer")]
    public async Task<ActionResult<IEnumerable<Policy>>> GetPolicies()
    {
        var isAdmin = User.HasClaim("https://policyengine/roles", "Policy.Admin");

        var query = _context.Policies.AsQueryable();

        if (!isAdmin)
            query = query.Where(p => p.IsActive);

        return await query.ToListAsync();
    }

    // GET: api/policy/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "Policy.Viewer")]
    public async Task<ActionResult<Policy>> GetPolicy(Guid id)
    {
        var policy = await _context.Policies.SingleOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return NotFound();

        return Ok(policy);
    }

    // POST: api/policy
    // Admin only — create a new policy.
    [HttpPost]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<ActionResult<Policy>> CreatePolicy(PolicyDto input)
    {
        var policy = new Policy
        {
            Title = input.Title,
            Description = input.Description,
            IsActive = input.IsActive
        };

        _context.Policies.Add(policy);

        // SaveChangesAsync automatically injects TenantId, CreatedAt, CreatedBy
        // from the validated JWT claims via TenantProvider.
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPolicy), new { id = policy.Id }, policy);
    }

    // PUT: api/policy/{id}
    // Admin only — full update.
    [HttpPut("{id}")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<IActionResult> UpdatePolicy(Guid id, PolicyDto input)
    {
        // SingleOrDefaultAsync (not FindAsync) so the global tenant query filter
        // is applied — users cannot update policies owned by other tenants.
        var policy = await _context.Policies.SingleOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return NotFound();

        policy.Title = input.Title;
        policy.Description = input.Description;
        policy.IsActive = input.IsActive;

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

        return Ok(policy);
    }

    // PATCH: api/policy/{id}/toggle
    // Admin only — toggle the IsActive flag without sending the full policy body.
    // This is what the Blazor UI's Activate/Deactivate button calls.
    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var policy = await _context.Policies.SingleOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return NotFound();

        policy.IsActive = !policy.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { policy.Id, policy.IsActive });
    }

    // DELETE: api/policy/{id}
    // Admin only.
    [HttpDelete("{id}")]
    [Authorize(Policy = "Policy.Admin")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        // SingleOrDefaultAsync so the global tenant filter prevents cross-tenant deletes.
        var policy = await _context.Policies.SingleOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return NotFound();

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

