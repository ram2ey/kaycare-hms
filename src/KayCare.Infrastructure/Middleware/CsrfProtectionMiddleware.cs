using KayCare.Core.Constants;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace KayCare.Infrastructure.Middleware;

/// <summary>
/// Double-submit CSRF protection for cookie-authenticated requests. A request carrying an
/// explicit Authorization: Bearer header cannot be forged cross-site (the attacker has no way to
/// know the token value) so it's exempt — this keeps Swagger/tooling/an in-flight old frontend
/// build working unmodified during a rolling deploy.
/// </summary>
public class CsrfProtectionMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace
    };

    private readonly RequestDelegate _next;

    public CsrfProtectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var isSafeMethod  = SafeMethods.Contains(context.Request.Method);
        var hasAuthCookie = context.Request.Cookies.ContainsKey(AuthCookieNames.Token);

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        var hasBearerHeader = authHeader != null &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (!isSafeMethod && hasAuthCookie && !hasBearerHeader)
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "CSRF token missing or invalid." });
                return;
            }
        }

        await _next(context);
    }
}
