using System.Security.Claims;

namespace PolicyEngineDemo.Api.Middleware;

public class TestUserMiddleware
{
    private readonly RequestDelegate _next;

    public TestUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Look for a header called "X-Tenant" in the request
        var tenantHeader = context.Request.Headers["X-Tenant"].ToString();

        if (!string.IsNullOrEmpty(tenantHeader))
        {
            var claims = new[]
            {
                new Claim("tid", tenantHeader), // Our Tenant ID
                new Claim(ClaimTypes.NameIdentifier, "test-user-123") // Our User ID
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
