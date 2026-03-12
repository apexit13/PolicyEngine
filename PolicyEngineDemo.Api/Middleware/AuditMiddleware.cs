using Microsoft.IO;
using PolicyEngineDemo.Contracts.Constants;
using PolicyEngineDemo.Contracts.Data;
using PolicyEngineDemo.Contracts.Models;
using System.Diagnostics;

namespace PolicyEngineDemo.Api.Middleware;

// Sits in the pipeline after UseAuthentication/UseAuthorization so that
// User claims are available. Captures:
//   - Timestamp, method, endpoint
//   - UserId + TenantId from the authenticated principal
//   - Request body (POST and PUT only — buffered, then rewound for the controller)
//   - Response status code and elapsed milliseconds
//
// Writes each entry to both:
//   1. The AuditLogs SQL table (queryable, tenant-scoped)
//   2. Serilog structured log (rolling daily JSON files)
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    // Only capture bodies for mutating verbs — GET and DELETE have no body
    private static readonly HashSet<string> _bodyVerbs =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH" };

    // Skip logging for non-API routes (health checks, Scalar UI, etc.)
    private const string ApiPrefix = "/api/";

    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _streamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // Only audit API routes
        if (!context.Request.Path.StartsWithSegments(ApiPrefix.TrimEnd('/')))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var endpoint = context.Request.Path.Value ?? "";

        // ── Capture request body ────────────────────────────────────────────
        string? requestBody = null;
        if (_bodyVerbs.Contains(method))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            // Rewind so the controller can still read it
            context.Request.Body.Position = 0;

            // Truncate very large bodies to avoid bloating the DB
            if (requestBody?.Length > 4000)
                requestBody = requestBody[..4000] + "…[truncated]";
        }

        // ── Run the rest of the pipeline ────────────────────────────────────
        await _next(context);
        sw.Stop();

        // ── Read user/tenant from claims (available after UseAuthorization) ─
        var userId = context.User?.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantId = context.User?.FindFirst(
            ClaimNames.TenantId)?.Value;

        var statusCode = context.Response.StatusCode;
        var durationMs = sw.ElapsedMilliseconds;

        // ── Write to database ────────────────────────────────────────────────
        var entry = new AuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            Method       = method,
            Endpoint     = endpoint,
            UserId       = userId,
            TenantId     = tenantId,
            RequestBody  = requestBody,
            StatusCode   = statusCode,
            DurationMs   = durationMs,
        };

        try
        {
            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never let audit failure break the main request
            _logger.LogError(ex, "Failed to write audit log to database");
        }

        // ── Write to Serilog ─────────────────────────────────────────────────
        _logger.LogInformation(
            "AUDIT {Method} {Endpoint} | tenant={TenantId} user={UserId} | {StatusCode} {DurationMs}ms | body={RequestBody}",
            method,
            endpoint,
            tenantId ?? "anon",
            userId   ?? "anon",
            statusCode,
            durationMs,
            requestBody ?? "-");
    }
}
