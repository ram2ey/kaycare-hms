using Microsoft.AspNetCore.Http;

namespace KayCare.Infrastructure.Middleware;

/// <summary>
/// Enforces the `mustChangePassword` JWT claim server-side. Without this, a user holding only
/// a temporary/reset password could use any endpoint their role allows for the full life of the
/// token, since the claim was previously only read by the frontend for display purposes.
/// </summary>
public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var mustChange = context.User.FindFirst("mustChangePassword")?.Value;
            if (string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase) && !IsAllowedWhilePasswordChangeIsPending(context))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Password change required before continuing." });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAllowedWhilePasswordChangeIsPending(HttpContext context)
    {
        // /me is the SPA's "am I logged in" boot check (the httpOnly cookie can't be read by JS)
        // and /logout must always succeed — neither can be gated behind change-password first,
        // or a must-change-password user gets misread as "not logged in" and can never reach the
        // change-password screen through normal navigation.
        if (context.Request.Method == HttpMethods.Get &&
            context.Request.Path.StartsWithSegments("/api/Auth/me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.Request.Method == HttpMethods.Post &&
            context.Request.Path.StartsWithSegments("/api/Auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.Request.Method != HttpMethods.Post)
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments("/api/Auth/change-password", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/api/Auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
