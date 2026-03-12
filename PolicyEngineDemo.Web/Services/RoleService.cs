using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using PolicyEngineDemo.Shared.Constants;
using System.Text.Json;

namespace PolicyEngineDemo.Web.Services;

// Reads roles from the access token at render time.
// Auth0 puts roles in the access token only (not ID token), so we can't use
// the standard ClaimsPrincipal roles. Instead we decode the JWT ourselves
// after auth is complete — safe to do in components, not during initialization.
public class RoleService
{
    private readonly IAccessTokenProvider _tokenProvider;

    public RoleService(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public async Task<bool> IsAdminAsync()
    {
        var roles = await GetRolesAsync();
        return roles.Contains("Policy.Admin");
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync()
    {
        try
        {
            var result = await _tokenProvider.RequestAccessToken();
            if (!result.TryGetToken(out var token))
                return [];

            var payload = ParseJwtPayload(token.Value);
            if (payload is null)
                return [];

            foreach (var prop in payload.Value.EnumerateObject())
            {
                if (prop.Name != ClaimNames.Roles)
                    continue;

                if (prop.Value.ValueKind == JsonValueKind.Array)
                    return prop.Value.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => s.Length > 0)
                        .ToList();

                if (prop.Value.ValueKind == JsonValueKind.String)
                    return [prop.Value.GetString() ?? ""];
            }
        }
        catch { }

        return [];
    }

    private static JsonElement? ParseJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;

            var base64 = parts[1].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return JsonDocument.Parse(Convert.FromBase64String(base64)).RootElement;
        }
        catch { return null; }
    }
}
