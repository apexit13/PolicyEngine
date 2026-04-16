using System.Security.Claims;

namespace PolicyEngine.Authorization.Services
{
    public interface IPermissionService
    {
        bool HasPermission(ClaimsPrincipal user, string permission);
        IEnumerable<string> GetPermissions(ClaimsPrincipal user);
    }
}