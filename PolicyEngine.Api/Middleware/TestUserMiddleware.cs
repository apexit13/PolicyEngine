using System.Security.Claims;
using PolicyEngine.Authorization.Constants;

namespace PolicyEngine.Api.Middleware;

public class TestUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public TestUserMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get tenant from header or use default
        var tenantHeader = context.Request.Headers["X-Tenant"].ToString();
        if (string.IsNullOrEmpty(tenantHeader))
            tenantHeader = _config["TestUser:DefaultTenant"] ?? "bchydro";

        // Get permission set from header or use default
        var permissionSet = context.Request.Headers["X-Permissions"].ToString();
        if (string.IsNullOrEmpty(permissionSet))
            permissionSet = _config["TestUser:DefaultPermissionSet"] ?? "admin";

        var permissions = GetPermissionsForSet(permissionSet);

        var claims = new List<Claim>
        {
            new(AuthClaimTypes.TenantId, tenantHeader),
            new(AuthClaimTypes.UserId, "test-user-123"),
        };

        // Add each permission as an individual claim
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AuthClaimTypes.Permissions, permission));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }

    private static string[] GetPermissionsForSet(string permissionSet)
    {
        return permissionSet.ToLowerInvariant() switch
        {
            "admin" =>
            [
                Permissions.ManagePolicies,
                Permissions.ReadPolicies,
                Permissions.ReadDashboard
            ],
            "viewer" =>
            [
                Permissions.ReadPolicies
            ],
            _ => []
        };
    }
}