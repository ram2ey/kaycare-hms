using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, ITenantContext tenantContext)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        string? identifier;
        Guid? claimTenantId = null;

        if (isAuthenticated)
        {
            // Once a request is authenticated, the tenant is authoritatively determined by the
            // JWT's tenantId claim. A client-supplied X-Tenant-Code header must never be able to
            // override this — otherwise any authenticated user of any tenant could read or write
            // another tenant's data simply by sending a different header. The header/subdomain
            // path below is only for resolving a tenant before a JWT exists (e.g. login).
            var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var parsedId))
            {
                claimTenantId = parsedId;
            }
            identifier = null;
        }
        else
        {
            identifier = ResolveIdentifier(context);
        }

        if (string.IsNullOrEmpty(identifier) && !claimTenantId.HasValue)
        {
            // No tenant header/subdomain or authenticated claims — allow the request through.
            // Auth middleware will reject protected endpoints.
            await _next(context);
            return;
        }

        Tenant? tenant = null;
        if (!string.IsNullOrEmpty(identifier))
        {
            tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantCode == identifier || t.Subdomain == identifier);
        }
        else if (claimTenantId.HasValue)
        {
            tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == claimTenantId.Value);
        }

        if (tenant is null || !tenant.IsActive)
        {
            var displayId = identifier ?? claimTenantId?.ToString() ?? "unknown";
            _logger.LogWarning("Tenant not found or inactive: {Identifier}", displayId);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{displayId}' not found or inactive." });
            return;
        }

        tenantContext.TenantId = tenant.TenantId;
        tenantContext.TenantCode = tenant.TenantCode;

        await _next(context);
    }

    private static string? ResolveIdentifier(HttpContext context)
    {
        // X-Tenant-Code header takes precedence (local dev + API clients)
        var header = context.Request.Headers["X-Tenant-Code"].FirstOrDefault();
        if (!string.IsNullOrEmpty(header)) return header;

        // Subdomain: stmarys.kaycare.com → "stmarys"
        // Ignore Azure default domains (*.azurewebsites.net, *.azurestaticapps.net)
        var host = context.Request.Host.Host;
        if (host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".azurestaticapps.net", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = host.Split('.');
        if (parts.Length >= 3) return parts[0];

        return null;
    }
}
