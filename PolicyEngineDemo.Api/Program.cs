using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PolicyEngineDemo.Api.Middleware;
using PolicyEngineDemo.Api.Services;
using PolicyEngineDemo.Shared.Constants;
using PolicyEngineDemo.Shared.Data;
using PolicyEngineDemo.Shared.Interfaces;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// ── SERILOG ─────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/audit-.json",
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50_000_000)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Prevent .NET from mapping short OIDC claims (like 'sub' or 'tid') 
// to legacy Microsoft XML namespaces. This ensures our Claims match 
// the exact keys coming from Auth0.
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ── AUTHENTICATION ──────────────────────────────────────────────────────────
// Validates Auth0-issued JWTs on every request.
// Auth0 signs tokens with RS256 — the public key is fetched automatically
// from the Auth0 JWKS endpoint (no secret needed).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority is your Auth0 domain — ASP.NET Core fetches the OIDC
        // discovery document from here to validate tokens automatically.
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";

        // Audience must match the Identifier set when registering the API in Auth0.
        options.Audience = builder.Configuration["Auth0:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// ── AUTHORIZATION ───────────────────────────────────────────────────────────
// Maps the custom roles claim injected by the Auth0 Action into ASP.NET Core
// roles so [Authorize(Roles = "Policy.Admin")] works out of the box.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Policy.Admin", policy =>
        policy.RequireClaim(ClaimNames.Roles, "Policy.Admin"));

    options.AddPolicy("Policy.Viewer", policy =>
        policy.RequireClaim(ClaimNames.Roles, "Policy.Viewer", "Policy.Admin"));
});

// CORS is just implemented for local dev.
// For production, Azure configures CORS in front of the API and only allows the Blazor client origin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins(
                "https://white-bay-09fb46b0f.4.azurestaticapps.net",
                "https://localhost:5068",
                "https://localhost:7026")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddSingleton<Microsoft.IO.RecyclableMemoryStreamManager>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Mock middleware stays active in Development so we can still test
    // locally via Scalar with the X-Tenant header — no Auth0 token needed.
    app.UseMiddleware<TestUserMiddleware>();

    // Azure App Service handles HTTPS termination itself so only needed in Development
    app.UseHttpsRedirection();
    app.UseCors("BlazorClient");
}

app.MapOpenApi();
app.MapScalarApiReference();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.MapControllers();
app.Run();
