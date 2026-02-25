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
            Description = input.Description
        };

        _context.Policies.Add(policy);

        // SaveChangesAsync will automatically inject TenantId, CreatedAt, and CreatedBy
        await _context.SaveChangesAsync();

        return Ok(policy);
    }
}

public record PolicyDto(string Title, string Description);
