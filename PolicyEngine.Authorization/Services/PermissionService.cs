using System.Security.Claims;
using System.Text.Json;
using PolicyEngine.Authorization.Constants;

namespace PolicyEngine.Authorization.Services
{
    public class PermissionService : IPermissionService
    {
        public bool HasPermission(ClaimsPrincipal user, string permission)
        {
            if (user == null || string.IsNullOrEmpty(permission))
                return false;

            var permissions = GetPermissions(user);
            return permissions.Contains(permission);
        }

        public IEnumerable<string> GetPermissions(ClaimsPrincipal user)
        {
            if (user == null)
                return Enumerable.Empty<string>();

            var permissionsClaim = user.FindFirst(c => c.Type == AuthClaimTypes.Permissions)?.Value;

            if (string.IsNullOrEmpty(permissionsClaim))
                return Enumerable.Empty<string>();

            try
            {
                return JsonSerializer.Deserialize<string[]>(permissionsClaim) ?? Enumerable.Empty<string>();
            }
            catch (JsonException)
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}