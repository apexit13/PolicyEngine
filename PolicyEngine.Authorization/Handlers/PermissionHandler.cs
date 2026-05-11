using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using PolicyEngine.Authorization.Requirements;
using PolicyEngine.Authorization.Constants;

namespace PolicyEngine.Authorization.Handlers
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // For API: Check the standard permissions claim (no namespace)
            var permissionsClaim = context.User.FindFirst(c => c.Type == AuthClaimTypes.Permissions)?.Value;

            // For Web client: Check the namespaced permissions claim
            if (string.IsNullOrEmpty(permissionsClaim))
            {
                permissionsClaim = context.User.FindFirst(c => c.Type == AuthClaimTypes.PermissionsPolicyEngineURI)?.Value;
            }

            if (string.IsNullOrEmpty(permissionsClaim))
            {
                return Task.CompletedTask;
            }

            try
            {
                var permissions = JsonSerializer.Deserialize<string[]>(permissionsClaim) ?? [permissionsClaim];

                if (permissions != null && permissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing permissions: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}