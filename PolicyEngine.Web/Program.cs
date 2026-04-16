using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PolicyEngine.Authorization.Constants;
using PolicyEngine.Authorization.Extensions;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Web;
using PolicyEngine.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── AUTHENTICATION ──────────────────────────────────────────────────────────
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority =
        $"https://{builder.Configuration["Auth0:Domain"]}";

    options.ProviderOptions.ClientId = builder.Configuration["Auth0:ClientId"];

    var audience = builder.Configuration["Auth0:Audience"];

    if (!string.IsNullOrWhiteSpace(audience))
    {
        options.ProviderOptions.AdditionalProviderParameters
            .Add("audience", audience);
    }
    options.ProviderOptions.ResponseType = "code";

    options.ProviderOptions.DefaultScopes.Clear();
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
}).AddAccountClaimsPrincipalFactory<ArrayToClaimsPrincipalFactory<RemoteUserAccount>>();

// ── AUTHORIZATION ───────────────────────────────────────────────────────────
builder.Services.AddPermissionAuthorization();
builder.Services.AddAuthorizationCore(options =>
{
    //options.AddPermissionPolicies();

    // Note: Use the full namespace string as the claim type
    options.AddPolicy("ReadPolicies", policy =>
        policy.RequireClaim(AuthClaimTypes.PermissionsPolicyEngineURI, Permissions.ReadPolicies));

    options.AddPolicy("ManagePolicies", policy =>
        policy.RequireClaim(AuthClaimTypes.PermissionsPolicyEngineURI, Permissions.ManagePolicies));
    
    options.AddPolicy("ReadDashboard", policy =>
        policy.RequireClaim(AuthClaimTypes.PermissionsPolicyEngineURI, Permissions.ReadDashboard));
});

// ── HTTP CLIENT ─────────────────────────────────────────────────────────────
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7058";

builder.Services.AddHttpClient("AuthorizedClient",
    client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp =>
        sp.GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(
                authorizedUrls: [apiBaseUrl],
                scopes: ["openid", "profile", "email"]));

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>()
      .CreateClient("AuthorizedClient"));

// ── APP SERVICES ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();

// ── MUDBLAZOR ───────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

await builder.Build().RunAsync();