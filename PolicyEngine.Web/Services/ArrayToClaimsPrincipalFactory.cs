using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using PolicyEngine.Shared.Constants;
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

            if (account != null
                && account.AdditionalProperties.TryGetValue(ClaimNames.Roles, out var roles)
                && roles is JsonElement roleElement
                && roleElement.ValueKind == JsonValueKind.Array)
            {
                // Remove the raw array claim so it doesn't confuse the system
                var rawClaims = claimsIdentity.FindAll(ClaimNames.Roles).ToList();
                foreach (var rc in rawClaims) claimsIdentity.RemoveClaim(rc);

                // Add each role as its own individual claim
                foreach (var role in roleElement.EnumerateArray())
                {
                    var roleString = role.GetString();
                    if (!string.IsNullOrEmpty(roleString))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimNames.Roles, roleString));
                    }
                }
            }
            return user;
        }
    }

}
