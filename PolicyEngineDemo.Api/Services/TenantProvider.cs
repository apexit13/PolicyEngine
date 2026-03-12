using System.Security.Claims;
using PolicyEngineDemo.Shared.Constants;
using PolicyEngineDemo.Shared.Interfaces;

namespace PolicyEngineDemo.Api.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Pulls the 'tenant_id' claim from the JWT
    public string? TenantId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimNames.TenantId);

    // Pulls the current user's identifier claim from the JWT
    public string? UserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

