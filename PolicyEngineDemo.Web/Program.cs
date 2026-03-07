using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PolicyEngineDemo.Web;
using PolicyEngineDemo.Web.Services;

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
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<RoleService>();

// ── MUDBLAZOR ───────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

await builder.Build().RunAsync();