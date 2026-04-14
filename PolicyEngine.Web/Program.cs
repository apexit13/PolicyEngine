using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PolicyEngine.Web.Services;
using PolicyEngine.Shared.Constants;
using PolicyEngine.Shared.Interfaces;
using PolicyEngine.Web;

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

    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
}).AddAccountClaimsPrincipalFactory<ArrayToClaimsPrincipalFactory<RemoteUserAccount>>();

// ── AUTHORIZATION ───────────────────────────────────────────────────────────
// Maps the custom roles claim injected by the Auth0 Action into ASP.NET Core
// roles so [Authorize(Roles = "Admin")] works out of the box.
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireClaim(ClaimType.Roles, UserRole.Admin));

    options.AddPolicy("Viewer", policy =>
        policy.RequireClaim(ClaimType.Roles, UserRole.Viewer));
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