using System.Security.Claims;
using PolicyEngineDemo.Contracts.Interfaces;

namespace PolicyEngineDemo.Api.Services;

public class TenantProvider : ITenantProvider
{
    private const string ClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Pulls the 'tid' (Tenant ID) claim from the JWT
    public string? TenantId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimType);

    // Pulls the 'sub' (Subject/User ID) claim from the JWT
    public string? UserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

