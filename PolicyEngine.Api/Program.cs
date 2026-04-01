using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PolicyEngine.Api.Middleware;
using PolicyEngine.Api.Services;
using PolicyEngine.Core.Data;
using PolicyEngine.Core.Services;
using PolicyEngine.Shared.Constants;
using PolicyEngine.Shared.Interfaces;
using Scalar.AspNetCore;
using Serilog;

// ── SERILOG ─────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger(); // Catch early errors

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services) // Allows using DI services in sinks/enrichers
        .Enrich.FromLogContext());

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

    // ── CORS ──────────────────────────────────────────────────────────
    var AllowedOrigins = "BlazorClient";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowedOrigins, policy =>
        {
            policy
                .WithOrigins(
                    "https://white-bay-09fb46b0f.4.azurestaticapps.net",
                    "http://localhost:5068",
                    "https://localhost:7026")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // Use rate limiter to protect the API from abuse and prevent noisy neighbors in a multi-tenant environment.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Default policy — applies to all endpoints
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.FindFirst(ClaimNames.TenantId)?.Value
                         ?? context.Connection.RemoteIpAddress?.ToString()
                         ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 100,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            });
        });

        // Stricter policy for write operations
        options.AddPolicy("writes", context =>
        {
            var userId = context.User.FindFirst(ClaimNames.TenantId)?.Value
                         ?? context.Connection.RemoteIpAddress?.ToString()
                         ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter($"writes:{userId}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 20,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            });
        });
    });

    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddApplicationInsightsTelemetry();
    }

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IPolicyService, PolicyService>();
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

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Policy Engine API");
        });
    }

    app.UseHttpsRedirection();
    app.UseCors(AllowedOrigins);
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<AuditMiddleware>();
    app.UseRateLimiter();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are written before exit
}
