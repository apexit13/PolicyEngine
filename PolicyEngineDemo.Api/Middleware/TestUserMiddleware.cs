using System.Security.Claims;
using PolicyEngineDemo.Shared.Constants;

namespace PolicyEngineDemo.Api.Middleware;

public class TestUserMiddleware
{
    private readonly RequestDelegate _next;

    public TestUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantHeader = context.Request.Headers["X-Tenant"].ToString();

        if (!string.IsNullOrEmpty(tenantHeader))
        {
            // X-Role header lets you test both roles in Scalar:
            //   X-Role: Policy.Admin   → full access
            //   X-Role: Policy.Viewer  → read only
            //   (omit)                 → defaults to Policy.Admin for convenience
            var role = context.Request.Headers["X-Role"].ToString();
            if (string.IsNullOrEmpty(role))
                role = "Policy.Admin";

            var claims = new[]
            {
                new Claim("tenant_id", tenantHeader),
                new Claim(ClaimTypes.NameIdentifier, "test-user-123"),

                // Must match the namespace used in Program.cs authorization policies
                new Claim(ClaimNames.Roles, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}