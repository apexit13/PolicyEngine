using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using PolicyEngineDemo.Web.Models;

// Update the class definition to be generic
public class ArrayToClaimsPrincipalFactory<TAccount> : AccountClaimsPrincipalFactory<TAccount>
    where TAccount : RemoteUserAccount
{
    public ArrayToClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor) { }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        TAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);
        var claimsIdentity = (ClaimsIdentity)user.Identity;

        if (account != null && claimsIdentity != null)
        {
            // Map roles from AdditionalProperties (avoids the null account issue)
            if (account.AdditionalProperties.TryGetValue("https://policyengine", out var roles))
            {
                if (roles is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        claimsIdentity.AddClaim(new Claim(options.RoleClaim, item.GetString()));
                    }
                }
            }
        }
        return user;
    }
}
