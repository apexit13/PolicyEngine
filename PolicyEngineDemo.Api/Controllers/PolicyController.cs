using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicyEngineDemo.Core.Data;
using PolicyEngineDemo.Core.Models;

namespace PolicyEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly AppDbContext _context;

    public PolicyController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/policy
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Policy>>> GetPolicies()
    {
        return await _context.Policies.ToListAsync();
    }

    // POST: api/policy
    [HttpPost]
    public async Task<ActionResult<Policy>> CreatePolicy(PolicyDto input)
    {
        var policy = new Policy
        {
            Title = input.Title,
            Description = input.Description,
            IsActive = true
        };

        _context.Policies.Add(policy);

        // SaveChangesAsync will automatically inject TenantId, CreatedAt, and CreatedBy
        await _context.SaveChangesAsync();

        return Ok(policy);
    }

    // PUT: api/policy/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePolicy(Guid id, PolicyDto input)
    {
        // Use a LINQ query instead of FindAsyn(id) so the global tenant query filter is applied and
        // users cannot access entities from other tenants
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

    // DELETE: api/policy/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        // Use a LINQ query instead of FindAsyn(id) so the global tenant query filter is applied and
        // users cannot delete entities owned by other tenants.
        var policy = await _context.Policies.SingleOrDefaultAsync(p => p.Id == id);
        if (policy == null)
            return NotFound();

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record PolicyDto(string Title, string Description, bool IsActive);
