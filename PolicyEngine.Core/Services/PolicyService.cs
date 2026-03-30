using Microsoft.EntityFrameworkCore;
using PolicyEngine.Core.Data;
using PolicyEngine.Core.Models;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Shared.Requests;
using PolicyEngine.Shared.Responses;

namespace PolicyEngine.Core.Services;

/// <summary>
/// EF Core implementation of IPolicyService.
/// Registered in PolicyEngine.Api via Program.cs.
/// Tenant isolation is handled automatically by AppDbContext's global query filter.
/// </summary>
public class PolicyService : IPolicyService
{
    private readonly AppDbContext _context;

    public PolicyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PolicyResponse>> GetPoliciesAsync()
    {
        var policies = await _context.Policies
            .AsNoTracking()
            .ToListAsync();

        return policies.Select(MapToResponse).ToList();
    }

    public async Task<PolicyResponse?> GetPolicyAsync(Guid id)
    {
        var policy = await _context.Policies
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id);

        return policy is null ? null : MapToResponse(policy);
    }

    public async Task<PolicyResponse?> CreatePolicyAsync(CreatePolicyRequest request)
    {
        var policy = new Policy
        {
            Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();

        return MapToResponse(policy);
    }

    public async Task<PolicyResponse?> UpdatePolicyAsync(Guid id, UpdatePolicyRequest request)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return null;

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
                return null;
            throw;
        }

        return MapToResponse(policy);
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return;

        policy.IsActive = !policy.IsActive;
        await _context.SaveChangesAsync();
    }

    public async Task DeletePolicyAsync(Guid id)
    {
        var policy = await _context.Policies
            .SingleOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return;

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();
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
