using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PolicyEngineDemo.Api.Middleware;
using PolicyEngineDemo.Api.Services;
using PolicyEngineDemo.Contracts.Data;
using PolicyEngineDemo.Contracts.Interfaces;
using Scalar.AspNetCore;
using System.Security.Claims;

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
        policy.RequireClaim("https://policyengine/roles", "Policy.Admin"));

    options.AddPolicy("Policy.Viewer", policy =>
        policy.RequireClaim("https://policyengine/roles", "Policy.Viewer", "Policy.Admin"));
});

// ── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                         ?? ["https://localhost:5001"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddSingleton<Microsoft.IO.RecyclableMemoryStreamManager>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// ── PIPELINE ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Mock middleware stays active in Development so you can still test
    // locally via Scalar with the X-Tenant header — no Auth0 token needed.
    app.UseMiddleware<TestUserMiddleware>();

    app.MapOpenApi();

    // Azure App Service handles HTTPS termination itself so only needed in Development
    app.UseHttpsRedirection();
}

app.MapScalarApiReference();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();
app.MapControllers();
app.Run();
