using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using PolicyEngine.Authorization.Constants;
using System.Security.Claims;
using System.Text.Json;

namespace PolicyEngine.Web.Services
{
    public class ArrayToClaimsPrincipalFactory<TAccount> : AccountClaimsPrincipalFactory<TAccount> where TAccount : RemoteUserAccount
    {
        public ArrayToClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor) { }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(TAccount account, RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);
            var claimsIdentity = user.Identity as ClaimsIdentity;
            if (claimsIdentity == null) return user;

            if (account != null)
            {
                // ── ROLES ────────────────────────────────────────────────────────
                if (account.AdditionalProperties.TryGetValue(AuthClaimTypes.Roles, out var roles)
                    && roles is JsonElement roleElement
                    && roleElement.ValueKind == JsonValueKind.Array)
                {
                    var rawClaims = claimsIdentity.FindAll(AuthClaimTypes.Roles).ToList();
                    foreach (var rc in rawClaims) claimsIdentity.RemoveClaim(rc);

                    foreach (var role in roleElement.EnumerateArray())
                    {
                        var roleString = role.GetString();
                        if (!string.IsNullOrEmpty(roleString))
                            claimsIdentity.AddClaim(new Claim(AuthClaimTypes.Roles, roleString));
                    }
                }
                // ── PERMISSIONS ────────────────────────────────────────────────────────
                if (account.AdditionalProperties.TryGetValue(AuthClaimTypes.PermissionsPolicyEngineURI, out var permissions)
                    && permissions is JsonElement permissionsElement
                    && permissionsElement.ValueKind == JsonValueKind.Array)
                {
                    var rawClaims = claimsIdentity.FindAll(AuthClaimTypes.PermissionsPolicyEngineURI).ToList();
                    foreach (var rc in rawClaims) claimsIdentity.RemoveClaim(rc);

                    foreach (var permission in permissionsElement.EnumerateArray())
                    {
                        var permissionString = permission.GetString();
                        if (!string.IsNullOrEmpty(permissionString))
                            claimsIdentity.AddClaim(new Claim(AuthClaimTypes.PermissionsPolicyEngineURI, permissionString));
                    }
                }
            }

            return user;
        }
    }

}
