using System.Security.Claims;
using PolicyEngineDemo.Core.Interfaces;

namespace PolicyEngineDemo.Api.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Pulls the 'tid' (Tenant ID) claim from the JWT
    public string? GetTenantId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("tid");

    // Pulls the 'sub' (Subject/User ID) claim from the JWT
    public string? GetUserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

